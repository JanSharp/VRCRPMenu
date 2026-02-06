using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipMovement : NoClipMovementAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;
        [HideInInspector][SerializeField][SingletonReference] private QuickDebugUI qd;

        private float horizontalInput;
        private float verticalInput;
        /// <summary>
        /// <para>Meters per second.</para>
        /// </summary>
        private float speed;
        public override float Speed
        {
            get => speed;
            set => speed = value;
        }

        [Tooltip("Only applies when not near a collider.")]
        [SerializeField] private NoClipModeWhileStill modeWhileStill = NoClipModeWhileStill.FakeGround;
        public override NoClipModeWhileStill ModeWhileStill
        {
            get => modeWhileStill;
            set
            {
                if (modeWhileStill == value)
                    return;
                modeWhileStill = value;
                UpdateCurrentModeWhileStillEventName();
                currentMoveMode = InvalidMoveModeId; // Trigger the initialization logic of the standing still modes.
            }
        }

        [Tooltip("Also applies when standing still but being near a collider.")]
        [SerializeField] private NoClipModeWhileMoving modeWhileMoving = NoClipModeWhileMoving.Teleport;
        public override NoClipModeWhileMoving ModeWhileMoving
        {
            get => modeWhileMoving;
            set
            {
                modeWhileMoving = value;
                UpdateCurrentModeWhileMovingEventName();
            }
        }

        private string currentModeWhileStillEventName;
        private string currentModeWhileMovingEventName;

        private bool isNoClipActive;
        public override bool IsNoClipActive
        {
            get => isNoClipActive;
            set
            {
                if (isNoClipActive == value)
                    return;
                isNoClipActive = value;
                UpdateRegistration();
            }
        }

        private uint avoidTeleportingCounter = 0u;
        private bool avoidTeleporting = false;

        [SerializeField] private Transform fakeGround;
        [SerializeField] private GameObject fakeGroundGo;
        [SerializeField] private Collider fakeGroundCollider;
        [SerializeField] private float moveFakeGroundEveryDistance;
        [SerializeField] private LayerMask localPlayerLayer;
        private LayerMask localPlayerCollidingLayers;
        private Vector3 currentPosition;
        private Vector3 currentOffsetWithinPlaySpace;
        private float currentY;
        private Vector3 currentFakeGroundPosition;
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

        private const int InvalidMoveModeId = 0;
        private const int StillVelocityMoveModeId = 1;
        private const int StillFakeFloorMoveModeId = 2;
        private const int MovingVelocityMoveModeId = 3;
        private const int MovingTeleportMoveModeId = 4;

        private int currentMoveMode = InvalidMoveModeId;

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            localPlayerCollidingLayers = teleportManager.LocalPlayerCollidingLayers & ~localPlayerLayer;
            UpdateCurrentModeWhileStillEventName();
            UpdateCurrentModeWhileMovingEventName();
        }

        public override void IncrementAvoidTeleporting()
        {
            avoidTeleportingCounter++;
            avoidTeleporting = true;
        }

        public override void DecrementAvoidTeleporting()
        {
            if (avoidTeleportingCounter == 0u)
            {
                Debug.LogError($"[RPMenu] Attempt to {nameof(DecrementAvoidTeleporting)} more often than "
                    + $"{nameof(IncrementAvoidTeleporting)} on the {nameof(NoClipMovement)} script.");
                return;
            }
            avoidTeleporting = (--avoidTeleportingCounter) != 0u;
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

        private void UpdateRegistration()
        {
            // Setting Immobilize to true is a bad idea, especially noticeable when using pure velocity
            // without a collider to stand on, thus being in the falling animation, where your tracking data
            // head is entirely disconnected from the actual head. Moving your head to the side does not move
            // the avatar.
            if (isNoClipActive)
            {
                currentPosition = localPlayer.GetPosition();
                currentMoveMode = InvalidMoveModeId;
                var origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(origin, head.position);
                currentVelocity = localPlayer.GetVelocity(); // In case it registers and deregisters within the same frame.
                averageDeltaTime = Time.deltaTime;
                updateManager.Register(this);
            }
            else
            {
                fakeGroundGo.SetActive(false);
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

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!isNoClipActive || !value)
                return;
            // Preferring this to cancel jumping rather than setting jump impulse to 0 in order to not
            // conflict with any external systems which take control of the player's jump impulse,
            // avoiding forcing said systems to interact with the manager the RP menu would be using.
            Vector3 velocity = localPlayer.GetVelocity();
            velocity.y = 0f;
            localPlayer.SetVelocity(velocity);
        }

        /// <summary>
        /// <para>Used within <see cref="CustomUpdate"/>.</para>
        /// </summary>
        private VRCPlayerApi.TrackingData currentOrigin;
        /// <inheritdoc cref="currentOrigin"/>
        private VRCPlayerApi.TrackingData currentHead;
        /// <inheritdoc cref="currentOrigin"/>
        private bool currentIsNearColliders;

        public void CustomUpdate()
        {
            // Depending on what this shows while moving around using teleport, if it is false even though
            // the collider is kept under the player then there is no reason to have the collider under the
            // player... unless velocity is not set every frame while teleporting, then the collider is needed.
            // Observed this to be true while moving using teleport at least in desktop, which is a good start.
            qd.ShowForOneFrame(this, "IsPlayerGrounded", localPlayer.IsPlayerGrounded().ToString());
            qd.ShowForOneFrame(this, "GetVelocity", localPlayer.GetVelocity().ToString());

            averageDeltaTime = averageDeltaTime * AverageDeltaTimePrevFraction + Time.deltaTime * AverageDeltaTimeNewFraction;

            currentOrigin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            currentHead = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            currentVelocity = currentHead.rotation * ((Vector3.right * horizontalInput + Vector3.forward * verticalInput) * speed);

            currentIsNearColliders = CheckForColliders();

            if (currentVelocity == Vector3.zero && !currentIsNearColliders)
                SendCustomEvent(currentModeWhileStillEventName);
            else
                SendCustomEvent(currentModeWhileMovingEventName);
        }

        private void UpdateCurrentModeWhileStillEventName()
        {
            if (modeWhileStill == NoClipModeWhileStill.Velocity)
                currentModeWhileStillEventName = nameof(UpdateWhileStillUsingVelocity);
            else if (modeWhileStill == NoClipModeWhileStill.FakeGround)
                currentModeWhileStillEventName = nameof(UpdateWhileStillUsingFakeGround);
        }

        private void UpdateCurrentModeWhileMovingEventName()
        {
            if (modeWhileMoving == NoClipModeWhileMoving.Velocity)
                currentModeWhileMovingEventName = nameof(UpdateWhileMovingUsingVelocity);
            else if (modeWhileMoving == NoClipModeWhileMoving.Combo)
                currentModeWhileMovingEventName = nameof(UpdateWhileMovingUsingCombo);
            else if (modeWhileMoving == NoClipModeWhileMoving.Teleport)
                currentModeWhileMovingEventName = nameof(UpdateWhileMovingUsingTeleport);
        }

        public void UpdateWhileStillUsingVelocity()
        {
            currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(currentOrigin, currentHead.position);
            currentPosition = localPlayer.GetPosition();
            if (currentMoveMode != StillVelocityMoveModeId)
            {
                currentMoveMode = StillVelocityMoveModeId;
                fakeGroundGo.SetActive(false);
                localPlayer.SetGravityStrength(0f);
            }
            localPlayer.SetVelocity(Vector3.zero);
        }

        public void UpdateWhileStillUsingFakeGround()
        {
            currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(currentOrigin, currentHead.position);
            currentPosition = localPlayer.GetPosition();
            if (currentMoveMode != StillFakeFloorMoveModeId)
            {
                currentMoveMode = StillFakeFloorMoveModeId;
                fakeGroundGo.SetActive(true);
                localPlayer.SetGravityStrength(1f);
                currentY = currentPosition.y;
                currentFakeGroundPosition = currentPosition;
                fakeGround.position = currentFakeGroundPosition;
                localPlayer.SetVelocity(Vector3.zero);
            }
            else
            {
                currentPosition.y = currentY;
                if (Vector3.Distance(currentPosition, currentFakeGroundPosition) >= moveFakeGroundEveryDistance)
                {
                    // Moving the fake ground every frame causes the IK to flicker a ton while in full body.
                    // Only moving it when the player has moved a notable distance still causes a one frame
                    // flicker however it is so un noticeable that people are incredibly unlike to even
                    // realize it.
                    // The alternative would be having 4 fake ground colliders and shuffling them around such
                    // that the collider the player is standing on never gets moved.
                    currentFakeGroundPosition = currentPosition;
                    fakeGround.position = currentFakeGroundPosition;
                }
            }
            // Setting velocity to 0 every frame even while standing on ground does have negative side
            // effects, such as once you jump, getting you slightly off the ground, you never land.
            // Setting it to a negative y value every frame puts you into the falling animation.
            // So just leaving it untouched, and cancelling jumping using InputJump.
        }

        public void UpdateWhileMovingUsingVelocity()
        {
            currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(currentOrigin, currentHead.position);
            currentPosition = localPlayer.GetPosition();
            if (currentMoveMode != MovingVelocityMoveModeId)
            {
                currentMoveMode = MovingVelocityMoveModeId;
                fakeGroundGo.SetActive(false);
                localPlayer.SetGravityStrength(0f);
            }
            localPlayer.SetVelocity(currentVelocity);
        }

        public void UpdateWhileMovingUsingCombo()
        {
            if (currentIsNearColliders)
                UpdateWhileMovingUsingTeleport();
            else
                UpdateWhileMovingUsingVelocity();
        }

        public void UpdateWhileMovingUsingTeleport()
        {
            if (avoidTeleporting && currentVelocity == Vector3.zero)
            {
                // Specifically fake ground, not whatever still mode is configured, because fake ground more
                // so matches behavior expected from teleporting, as well as being clean for UI interaction.
                UpdateWhileStillUsingFakeGround();
                currentY = localPlayer.GetPosition().y; // Make sure the player does not get pushed under the fake ground.
                return;
            }
            if (currentMoveMode != MovingTeleportMoveModeId)
            {
                currentMoveMode = MovingTeleportMoveModeId;
                fakeGroundGo.SetActive(true);
                localPlayer.SetGravityStrength(1f);
            }
            Vector3 offsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(currentOrigin, currentHead.position);
            Vector3 movementWithinPlaySpace = currentOrigin.rotation * (offsetWithinPlaySpace - currentOffsetWithinPlaySpace);
            currentOffsetWithinPlaySpace = offsetWithinPlaySpace;
            currentPosition += currentVelocity * Mathf.Min(Time.deltaTime, MaxAcknowledgedTimeBetweenFrames) + movementWithinPlaySpace;
            fakeGround.position = currentPosition;
            teleportManager.MoveAndRetainHeadRotation(currentPosition);
            // TODO: set velocity to zero or not depending on a flag that's part of the API, which effectively
            // enables or disables locomotion animations while teleporting... though this depends on if the
            // is grounded flag is true or not, which has not been tested.
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
