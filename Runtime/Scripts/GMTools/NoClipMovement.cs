using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipMovement : NoClipMovementAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipManagerAPI noClipManager;
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        // TODO: Remove this and add an API to toggle the same behavior this is used for currently.
        [SerializeField][FindInParent] private MenuManagerAPI associatedMenuManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        private float horizontalInput;
        private float verticalInput;
        /// <summary>
        /// <para>Meters per second.</para>
        /// </summary>
        private float speed;

        [SerializeField] private Transform fakeGround;
        [SerializeField] private GameObject fakeGroundGo;
        [SerializeField] private Collider fakeGroundCollider;
        [SerializeField] private LayerMask localPlayerLayer;
        private LayerMask localPlayerCollidingLayers;
        private Vector3 currentPosition;
        private Vector3 currentOffsetWithinPlaySpace;
        private bool wasMoving;
        private float currentY;
        private Collider[] overlappingColliders = new Collider[2];
        private Vector3 currentVelocity;

        private const float MaxAcknowledgedTimeBetweenFrames = 0.2f;
        private const float AverageDeltaTimePrevFraction = 0.9f;
        private const float AverageDeltaTimeNewFraction = 1f - AverageDeltaTimePrevFraction;
        private float averageDeltaTime;
        /// <summary>
        /// <para>To prevent getting flung out of colliders due to switching movement modes too close to a
        /// wall.</para>
        /// </summary>
        private const float SafetyRadiusAroundPlayer = 0.3f;

        private VRCPlayerApi localPlayer;
        private bool hasMenuManager;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            localPlayerCollidingLayers = teleportManager.LocalPlayerCollidingLayers & ~localPlayerLayer;
            hasMenuManager = associatedMenuManager != null;
        }

        // TODO: replace this with an api too.
        [NoClipEvent(NoClipEventType.OnIsNoClipActiveChanged)]
        public void OnIsNoClipActiveChanged()
        {
            UpdateRegistration();
        }

        [RPMenuTeleportEvent(RPMenuTeleportEventType.OnLocalPlayerTeleported)]
        public void OnLocalPlayerTeleported()
        {
            currentPosition = localPlayer.GetPosition();
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;
            currentPosition = localPlayer.GetPosition();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => UpdateSpeed();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateSpeed();

        [NoClipEvent(NoClipEventType.OnLocalLatencyNoClipSpeedChanged)]
        public void OnLocalLatencyNoClipSpeedChanged() => UpdateSpeed();

        private void UpdateSpeed()
        {
            speed = noClipManager.LatencyNoClipSpeed;
        }

        private void UpdateRegistration()
        {
            // Setting Immobilize to true is a bad idea, especially noticeable when using pure velocity
            // without a collider to stand on, thus being in the falling animation, where your tracking data
            // head is entirely disconnected from the actual head. Moving your head to the side does not move
            // the avatar.
            if (noClipManager.IsNoClipActive)
            {
                currentPosition = localPlayer.GetPosition();
                fakeGround.position = currentPosition;
                fakeGroundGo.SetActive(true);
                var origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(origin, head.position);
                currentVelocity = localPlayer.GetVelocity(); // In case it registers and deregisters within the same frame.
                localPlayer.SetGravityStrength(0f); // TODO: Abstract this away into some manager.
                averageDeltaTime = Time.deltaTime;
                updateManager.Register(this);
            }
            else
            {
                fakeGroundGo.SetActive(false);
                localPlayer.SetGravityStrength(1f);
                localPlayer.SetVelocity(currentVelocity);
                updateManager.Deregister(this);
            }
        }

        // Have experimented with disabling the object to prevent getting these event when we do not currently
        // care about them, however disabled objects receive them just the same.
        // If that did work then the concern would be not having an up to date value when no clip gets
        // enabled, and the input value does not change thus not raising these input events, so disabling the
        // object not disabling the events avoids this issue inherently.

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            horizontalInput = value;
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            verticalInput = value;
        }

        public void CustomUpdate()
        {
            float deltaTime = Time.deltaTime;
            averageDeltaTime = averageDeltaTime * AverageDeltaTimePrevFraction + deltaTime * AverageDeltaTimeNewFraction;

            var origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            currentVelocity = head.rotation * ((Vector3.right * horizontalInput + Vector3.forward * verticalInput) * speed);

            bool isNearColliders = CheckForColliders();

            // Force not teleporting the player while the menu is open while standing still,
            // otherwise it jumps around like crazy and is un-interactable.
            if (currentVelocity == Vector3.zero && (!isNearColliders || (hasMenuManager && associatedMenuManager.IsMenuOpen)))
            {
                currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(origin, head.position);
                currentPosition = localPlayer.GetPosition();
                if (wasMoving)
                {
                    fakeGroundGo.SetActive(true);
                    wasMoving = false;
                    currentY = currentPosition.y;
                }
                else
                    currentPosition.y = currentY;
                fakeGround.position = currentPosition;
                // TODO: does setting velocity to zero every frame even while standing on the ground have any side effects?
                localPlayer.SetVelocity(Vector3.zero); // Cancel jumping.
                return;
            }
            wasMoving = true;

            if (isNearColliders)
            {
                fakeGroundGo.SetActive(true);
                Vector3 offsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(origin, head.position);
                Vector3 movementWithinPlaySpace = origin.rotation * (offsetWithinPlaySpace - currentOffsetWithinPlaySpace);
                currentOffsetWithinPlaySpace = offsetWithinPlaySpace;
                currentPosition += currentVelocity * Mathf.Min(deltaTime, MaxAcknowledgedTimeBetweenFrames) + movementWithinPlaySpace;
                fakeGround.position = currentPosition;
                teleportManager.MoveAndRetainHeadRotation(currentPosition);
                if (localPlayer.GetVelocity() != Vector3.zero)
                    Debug.Log("[RPMenuDebug] NoClipMovement  CustomUpdate (inner) - non zero velocity after teleport moving");
            }
            else
            {
                fakeGroundGo.SetActive(false);
                currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(origin, head.position);
                localPlayer.SetVelocity(currentVelocity);
                currentPosition = localPlayer.GetPosition();
            }
        }

        private bool CheckForColliders()
        {
            float radius = LocalPlayerCapsule.GetRadius();
            int count = Physics.OverlapCapsuleNonAlloc(
                currentPosition + Vector3.up * radius,
                currentPosition + Vector3.up * (LocalPlayerCapsule.GetHeight() - radius),
                radius + SafetyRadiusAroundPlayer + currentVelocity.magnitude * averageDeltaTime,
                overlappingColliders,
                localPlayerCollidingLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (overlappingColliders[i] != fakeGroundCollider)
                    return true;
            return false;
        }

        private Vector3 CalculateOffsetWithinPlaySpace(VRCPlayerApi.TrackingData origin, Vector3 headPosition)
        {
            // Must use the head tracking data, using player.GetPosition() is
            // unreliable due to that position getting pushed around by colliders.
            Vector3 offset = headPosition - origin.position;
            offset.y = 0f;
            return Quaternion.Inverse(origin.rotation) * offset;
        }
    }
}
