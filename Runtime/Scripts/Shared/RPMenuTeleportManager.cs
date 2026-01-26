using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(RPMenuTeleportEventAttribute), typeof(RPMenuTeleportEventType))]
    public class RPMenuTeleportManager : RPMenuTeleportManagerAPI
    {
        [SerializeField] public LayerMask localPlayerCollidingLayers;
        public override LayerMask LocalPlayerCollidingLayers => localPlayerCollidingLayers;

        // DEBUG
        public Transform debugOtherPlayer;
        public Transform debugLocalPlayer;

        private Quaternion[] directionsToTry = new Quaternion[]
        {
            Quaternion.identity,
            Quaternion.AngleAxis(60f, Vector3.up),
            Quaternion.AngleAxis(-60f, Vector3.up),
            Quaternion.AngleAxis(120f, Vector3.up),
            Quaternion.AngleAxis(-120f, Vector3.up),
            Quaternion.AngleAxis(180f, Vector3.up),
        };
        private int directionsToTryCount;

        private bool hasUndoData;
        /// <summary>
        /// <para>When <see langword="false"/> it means "is at redo able location".</para>
        /// </summary>
        private bool isAtUndoAbleLocation;
        private float undoAbleActionTakenAtTime;

        private bool redoAbleLocationIsPlayer;
        private CorePlayerData redoAblePlayer;
        private Vector3 redoAbleDesiredRelativeDirection;
        private Vector3 redoAblePosition;
        private Quaternion redoAbleRotation;

        private Vector3 undoAblePosition;
        private Quaternion undoAbleRotation;

        public override bool HasUndoData => hasUndoData;
        public override bool IsAtUndoAbleLocation => isAtUndoAbleLocation;
        public override float UndoAbleActionTakenAtTime => undoAbleActionTakenAtTime;
        public override bool RedoAbleLocationIsPlayer => redoAbleLocationIsPlayer;
        public override CorePlayerData RedoAblePlayer => redoAblePlayer;
        public override Vector3 RedoAbleDesiredRelativeDirection => redoAbleDesiredRelativeDirection;
        public override Vector3 RedoAblePosition => redoAblePosition;
        public override Quaternion RedoAbleRotation => redoAbleRotation;
        public override Vector3 UndoAblePosition => undoAblePosition;
        public override Quaternion UndoAbleRotation => undoAbleRotation;

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            directionsToTryCount = directionsToTry.Length;
        }

        private void Update()
        {
            // DEBUG
            FindTarget(debugOtherPlayer.position, debugOtherPlayer.rotation, Vector3.forward, out Vector3 position, out Quaternion rotation);
            debugLocalPlayer.SetPositionAndRotation(position, rotation);
        }

        private void RecordTeleportToPlayerUndo(Vector3 currentPosition, Quaternion currentRotation, CorePlayerData otherPlayer, Vector3 desiredRelativeDirection)
        {
            hasUndoData = true;
            isAtUndoAbleLocation = true;
            undoAbleActionTakenAtTime = Time.time;
            redoAbleLocationIsPlayer = true;
            redoAblePlayer = otherPlayer;
            redoAbleDesiredRelativeDirection = desiredRelativeDirection;
            undoAblePosition = currentPosition;
            undoAbleRotation = currentRotation;
            RaiseOnRPMenuTeleportUndoRedoStateChanged();
        }

        private void RecordTeleportToLocation(Vector3 currentPosition, Quaternion currentRotation, Vector3 otherPosition, Quaternion otherRotation)
        {
            hasUndoData = true;
            isAtUndoAbleLocation = true;
            undoAbleActionTakenAtTime = Time.time;
            redoAbleLocationIsPlayer = false;
            redoAblePosition = otherPosition;
            redoAbleRotation = otherRotation;
            undoAblePosition = currentPosition;
            undoAbleRotation = currentRotation;
            RaiseOnRPMenuTeleportUndoRedoStateChanged();
        }

        public override void TeleportToPlayer(CorePlayerData otherPlayer, Vector3 desiredRelativeDirection, bool recordUndo = false)
        {
            VRCPlayerApi playerApi = otherPlayer.playerApi;
            if (!Utilities.IsValid(playerApi))
                return;
            FindTarget(playerApi.GetPosition(), playerApi.GetRotation(), desiredRelativeDirection, out Vector3 position, out Quaternion rotation);

            Vector3 currentPosition = localPlayer.GetPosition();
            Quaternion currentRotation = localPlayer.GetRotation();
            TeleportWithoutLerp(position, rotation);
            if (recordUndo)
                RecordTeleportToPlayerUndo(currentPosition, currentRotation, otherPlayer, desiredRelativeDirection);
        }

        public void FindTarget(
            Vector3 otherPosition,
            Quaternion otherRotation,
            Vector3 desiredRelativeDirection,
            out Vector3 position,
            out Quaternion rotation)
        {
            float radius = LocalPlayerCapsule.GetRadius();
            float height = LocalPlayerCapsule.GetHeight() - radius;
            Vector3 centerPosition = otherPosition + Vector3.up * height;
            float longestDistance = -1f;
            Vector3 bestDirection = Vector3.zero;
            RaycastHit hit;
            for (int i = 0; i < directionsToTryCount; i++)
            {
                Vector3 direction = otherRotation * directionsToTry[i] * desiredRelativeDirection;
                if (!Physics.SphereCast(
                    centerPosition,
                    radius,
                    direction,
                    out hit,
                    DesiredDistanceFromOtherPlayers + SafetyDistanceFromWalls,
                    localPlayerCollidingLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    longestDistance = DesiredDistanceFromOtherPlayers;
                    bestDirection = direction;
                    break;
                }

                float distance = hit.distance - SafetyDistanceFromWalls;
                if (distance >= MinDistanceFromOtherPlayers)
                {
                    longestDistance = distance;
                    bestDirection = direction;
                    break;
                }

                if (distance > longestDistance)
                {
                    longestDistance = distance;
                    bestDirection = direction;
                }
            }

            position = centerPosition + bestDirection * longestDistance;
            if (Physics.SphereCast(
                position,
                radius,
                Vector3.down,
                out hit,
                height + MaxRelativeDownwardsDistance + SafetyDistanceFromGround,
                localPlayerCollidingLayers,
                QueryTriggerInteraction.Ignore))
            {
                position += Vector3.down * (hit.distance + radius - SafetyDistanceFromGround);
            }
            else
                position.y = otherPosition.y; // Potentially off a cliff, so stay on the same y as the target.

            Vector3 positionOnYPlane = position;
            positionOnYPlane.y = 0f;
            otherPosition.y = 0f;
            rotation = Quaternion.LookRotation(otherPosition - positionOnYPlane);
        }

        public override void TeleportTo(Vector3 position, Quaternion rotation, bool recordUndo = false)
        {
            Vector3 currentPosition = localPlayer.GetPosition();
            Quaternion currentRotation = localPlayer.GetRotation();
            TeleportWithoutLerp(position, rotation);
            if (recordUndo)
                RecordTeleportToLocation(currentPosition, currentRotation, position, rotation);
        }

        public override void UndoTeleport()
        {
            if (!hasUndoData || !isAtUndoAbleLocation)
                return;
            isAtUndoAbleLocation = false;
            TeleportWithoutLerp(undoAblePosition, undoAbleRotation);
            RaiseOnRPMenuTeleportUndoRedoStateChanged();
        }

        public override void RedoTeleport()
        {
            if (!hasUndoData || isAtUndoAbleLocation)
                return;
            isAtUndoAbleLocation = true;

            if (!redoAbleLocationIsPlayer)
            {
                TeleportWithoutLerp(redoAblePosition, redoAbleRotation);
                RaiseOnRPMenuTeleportUndoRedoStateChanged();
                return;
            }

            if (redoAblePlayer == null || redoAblePlayer.isDeleted)
                return;
            VRCPlayerApi playerApi = redoAblePlayer.playerApi;
            if (!Utilities.IsValid(playerApi))
                return;
            FindTarget(playerApi.GetPosition(), playerApi.GetRotation(), redoAbleDesiredRelativeDirection, out Vector3 position, out Quaternion rotation);
            TeleportWithoutLerp(position, rotation);
            RaiseOnRPMenuTeleportUndoRedoStateChanged();
        }

        private void TeleportWithoutLerp(Vector3 position, Quaternion rotation)
        {
            localPlayer.TeleportTo(position, rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: false);
            RaiseOnLocalPlayerTeleported();
        }

        public override Quaternion ProjectOntoYPlane(Quaternion rotation)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
            return projectedForward == Vector3.zero // Facing straight up or down?
                ? Quaternion.LookRotation(rotation * Vector3.down) // Imagine a head facing staring up. The chin is down.
                : Quaternion.LookRotation(projectedForward.normalized);
        }

        public override void MoveAndRetainHeadRotation(VRCPlayerApi player, Vector3 teleportPosition)
        {
            // TODO: Fix the jumping of rotation at that halfway downward angle.
            // Get head rotation => teleport => get head rotation again => calculate offset induced by teleport => corrective teleport.
            Quaternion playerRotation = player.GetRotation();
            Quaternion preHeadRotation = ProjectOntoYPlane(player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation);
            player.TeleportTo(teleportPosition, playerRotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: true);
            Quaternion postHeadRotation = ProjectOntoYPlane(player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation);
            Quaternion headRotationOffset = Quaternion.Inverse(postHeadRotation) * preHeadRotation;
            player.TeleportTo(teleportPosition, headRotationOffset * playerRotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: true);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalPlayerTeleportedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPMenuTeleportUndoRedoStateChangedListeners;

        private void RaiseOnLocalPlayerTeleported()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalPlayerTeleportedListeners, nameof(RPMenuTeleportEventType.OnLocalPlayerTeleported));
        }

        private void RaiseOnRPMenuTeleportUndoRedoStateChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPMenuTeleportUndoRedoStateChangedListeners, nameof(RPMenuTeleportEventType.OnRPMenuTeleportUndoRedoStateChanged));
        }

        #endregion
    }
}
