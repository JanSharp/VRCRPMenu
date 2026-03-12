using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(RPMenuTeleportEventAttribute), typeof(RPMenuTeleportEventType))]
    public class RPMenuTeleportManager : RPMenuTeleportManagerAPI
    {
        [SerializeField][HideInInspector][SingletonReference] private PlayerDataManagerAPI playerDataManager;

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

        private CorePlayerData lastPlayerTP;
        private Vector3 lastPlayerTPDesiredRelativeDirection;

        private bool hasPositionBeforeLastTP;
        private float timeAtPositionBeforeLastTP;
        private Vector3 positionBeforeLastTP;
        private Quaternion rotationBeforeLastTP;

        public override bool HasLastPlayerTP => lastPlayerTP != null && !lastPlayerTP.isDeleted;
        public override CorePlayerData LastPlayerTP => lastPlayerTP;
        public override Vector3 LastPlayerTPDesiredRelativeDirection => lastPlayerTPDesiredRelativeDirection;

        public override bool HasPositionBeforeLastTP => hasPositionBeforeLastTP;
        public override float TimeAtPositionBeforeLastTP => timeAtPositionBeforeLastTP;
        public override Vector3 PositionBeforeLastTP => positionBeforeLastTP;
        public override Quaternion RotationBeforeLastTP => rotationBeforeLastTP;

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            directionsToTryCount = directionsToTry.Length;
        }

#if RP_MENU_DEBUG
        private void Update()
        {
            // DEBUG
            FindTarget(debugOtherPlayer.position, debugOtherPlayer.rotation, Vector3.forward, out Vector3 position, out Quaternion rotation);
            debugLocalPlayer.SetPositionAndRotation(position, rotation);
        }
#endif

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            if (lastPlayerTP != playerDataManager.PlayerDataForEvent)
                return;
            lastPlayerTP = null;
            RaiseOnRPMenuLastPlayerTPStateChanged();
        }

        private void RecordTeleportToPlayer(Vector3 currentPosition, Quaternion currentRotation, CorePlayerData otherPlayer, Vector3 desiredRelativeDirection)
        {
            lastPlayerTP = otherPlayer;
            lastPlayerTPDesiredRelativeDirection = desiredRelativeDirection;
            RaiseOnRPMenuLastPlayerTPStateChanged();
            RecordTeleportToLocation(currentPosition, currentRotation);
        }

        private void RecordTeleportToLocation(Vector3 currentPosition, Quaternion currentRotation)
        {
            hasPositionBeforeLastTP = true;
            timeAtPositionBeforeLastTP = Time.time;
            positionBeforeLastTP = currentPosition;
            rotationBeforeLastTP = currentRotation;
            RaiseOnRPMenuRevertTPStateChanged();
        }

        public override void TeleportToPlayer(CorePlayerData otherPlayer, Vector3 desiredRelativeDirection, bool recordLastPlayerTPAndRevert = false)
        {
            VRCPlayerApi playerApi = otherPlayer.playerApi;
            if (!Utilities.IsValid(playerApi))
                return;
            FindTarget(playerApi.GetPosition(), playerApi.GetRotation(), desiredRelativeDirection, out Vector3 position, out Quaternion rotation);

            Vector3 currentPosition = localPlayer.GetPosition();
            Quaternion currentRotation = localPlayer.GetRotation();
            TeleportWithoutLerp(position, rotation);
            if (recordLastPlayerTPAndRevert)
                RecordTeleportToPlayer(currentPosition, currentRotation, otherPlayer, desiredRelativeDirection);
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

        public override void TeleportTo(Vector3 position, Quaternion rotation, bool recordRevert = false)
        {
            if (recordRevert)
                TeleportWithoutLerpAndRecordRevert(position, rotation);
            else
                TeleportWithoutLerp(position, rotation);
        }

        public override void RevertTeleport()
        {
            if (!hasPositionBeforeLastTP)
                return;
            TeleportWithoutLerpAndRecordRevert(positionBeforeLastTP, rotationBeforeLastTP);
        }

        public override void TeleportToLastPlayerTP()
        {
            if (!HasLastPlayerTP)
                return;
            VRCPlayerApi playerApi = lastPlayerTP.playerApi;
            if (!Utilities.IsValid(playerApi))
                return;
            FindTarget(playerApi.GetPosition(), playerApi.GetRotation(), lastPlayerTPDesiredRelativeDirection, out Vector3 position, out Quaternion rotation);
            TeleportWithoutLerpAndRecordRevert(position, rotation);
        }

        private void TeleportWithoutLerpAndRecordRevert(Vector3 position, Quaternion rotation)
        {
            Vector3 currentPosition = localPlayer.GetPosition();
            Quaternion currentRotation = localPlayer.GetRotation();
            TeleportWithoutLerp(position, rotation);
            RecordTeleportToLocation(currentPosition, currentRotation);
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

        private const int MaxTPIterations = 7;

        public override void MoveAndRetainHeadRotation(Vector3 targetPosition)
        {
            var origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            // Get head rotation => teleport => get head rotation again => calculate offset induced by teleport => corrective teleport.
            var initialHead = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            Quaternion initialHeadRotation = initialHead.rotation;
            Quaternion localPlayerRotation = localPlayer.GetRotation();
            localPlayer.TeleportTo(targetPosition, localPlayerRotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: true);
            Quaternion postHeadRotation = ProjectOntoYPlane(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation);
            Quaternion headRotationOffset = Quaternion.Inverse(postHeadRotation) * ProjectOntoYPlane(initialHeadRotation);
            localPlayer.TeleportTo(targetPosition, headRotationOffset * localPlayerRotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: true);
            if (localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation == initialHeadRotation)
                return;
            // The above returns successfully 99.9% of the time. However when the head is tilted to the left
            // or right, when looking up and down there is a single frame at some threshold where it requires
            // multiple iterations to fully undo unintentional movement and rotation induced by teleporting.
            // The lower the frame rate the fewer iterations it needs.
            // The less tilted the head is the fewer iterations it needs.
            // When setting MaxTPIterations to a lower value, such as 4 for example, it ends up doing
            // corrective iterations spread out across 2 frames interestingly enough, though at that point the
            // jump becomes visible and does not get cancelled out fully after the second frame either.
            // That means there is very little benefit to lowering MaxTPIterations, even though each iteration
            // is expensive. It takes a bit more than 1 ms on my machine with this current implementation.
            // Basically 0.25 ms per teleport.

            Vector3 headOffsetToOrigin = initialHead.position - origin.position;
            headOffsetToOrigin.y = 0f;
            Vector3 desiredOriginPos = targetPosition - headOffsetToOrigin;
            Quaternion desiredOriginRot = origin.rotation;
            Vector3 localPlayerPosition = localPlayer.GetPosition();
            localPlayerRotation = localPlayer.GetRotation(); // Reusing the initial value resulted in almost always doing all MaxTPIterations.
            int iterationCount = 0;
            for (int i = 0; i < MaxTPIterations; i++)
            {
                iterationCount = i + 1;
                // If this logic makes any sense to you, good job. I could not explain what this does.
                // I wrote this 6 months ago with tons of trial and error in an attempt at getting platform
                // attachment to work, see there for git history... though that won't explain much either.
                localPlayer.TeleportTo(localPlayerPosition, localPlayerRotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: true);
                origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                Vector3 posDiff = origin.position - desiredOriginPos;
                Quaternion rotDiff = Quaternion.Inverse(desiredOriginRot) * origin.rotation;
                Vector3 almostFinalPosition = localPlayerPosition - posDiff;
                Quaternion finalRotation = localPlayerRotation * Quaternion.Inverse(rotDiff);
                localPlayer.TeleportTo(almostFinalPosition, finalRotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: true);
                origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                Vector3 posDiff2 = origin.position - desiredOriginPos;
                localPlayer.TeleportTo(desiredOriginPos, desiredOriginRot, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, lerpOnRemote: true);
                localPlayer.TeleportTo(almostFinalPosition - posDiff2, finalRotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, lerpOnRemote: true);
                origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                if (origin.position == desiredOriginPos && desiredOriginRot == origin.rotation)
                    break;
            }
#if RP_MENU_DEBUG
            Debug.Log($"[RPMenuDebug] RPMenuTeleportManager  MoveAndRetainHeadRotation (inner) - iterationCount: {iterationCount}");
#endif
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalPlayerTeleportedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPMenuLastPlayerTPStateChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPMenuRevertTPStateChangedListeners;

        private void RaiseOnLocalPlayerTeleported()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalPlayerTeleportedListeners, nameof(RPMenuTeleportEventType.OnLocalPlayerTeleported));
        }

        private void RaiseOnRPMenuLastPlayerTPStateChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPMenuLastPlayerTPStateChangedListeners, nameof(RPMenuTeleportEventType.OnRPMenuLastPlayerTPStateChanged));
        }

        private void RaiseOnRPMenuRevertTPStateChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPMenuRevertTPStateChangedListeners, nameof(RPMenuTeleportEventType.OnRPMenuRevertTPStateChanged));
        }

        #endregion
    }
}
