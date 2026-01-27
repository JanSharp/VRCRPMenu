using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipMovement : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipManagerAPI noClipManager;
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        private float horizontalInput;
        private float verticalInput;
        private float speed;

        [SerializeField] private Transform fakeGround;
        private Vector3 currentPosition;
        private Vector3 currentOffsetWithinPlaySpace;

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }

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
            if (noClipManager.IsNoClipActive)
            {
                currentPosition = localPlayer.GetPosition();
                fakeGround.position = currentPosition;
                fakeGround.gameObject.SetActive(true);
                var origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
                var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                currentOffsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(origin, head.position);
                localPlayer.SetGravityStrength(0f);
                updateManager.Register(this);
            }
            else
            {
                fakeGround.gameObject.SetActive(false);
                localPlayer.SetGravityStrength(1f);
                localPlayer.SetVelocity(Vector3.zero); // TODO: Maybe track velocity and apply it here?
                updateManager.Deregister(this);
            }
        }

        // NOTE: Have experimented with disabling the object to prevent getting these event when we do not
        // currently care about them, however disabled objects receive them just the same.
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
            var origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            Vector3 offsetWithinPlaySpace = CalculateOffsetWithinPlaySpace(origin, head.position);
            Vector3 movementWithinPlaySpace = origin.rotation * (offsetWithinPlaySpace - currentOffsetWithinPlaySpace);
            currentOffsetWithinPlaySpace = offsetWithinPlaySpace;

            Quaternion headRotation = head.rotation;
            Vector3 forward = headRotation * Vector3.forward;
            Vector3 right = headRotation * Vector3.right;
            currentPosition += (forward * verticalInput + right * horizontalInput) * speed * Mathf.Min(Time.deltaTime, 0.2f)
                + movementWithinPlaySpace;
            fakeGround.position = currentPosition;
            // TODO: Use different movement method (not teleportation) while not inside of a collider.
            // TODO: Force use different movement method even while in collider when menu is open, while standing still.
            // TODO: figure out if there is anything that can be done to prevent this weird state of the
            // player's avatar being suck constantly moving, where cancelling no clip clearly makes you move to the side.
            teleportManager.MoveAndRetainHeadRotation(localPlayer, currentPosition);

            // // Must set this every frame, otherwise even though it is not visible the player does constantly fall,
            // // which is ultimately noticeable by the dummy canvases around you in desktop while having the UI open flickering.
            // // If there is a collider kept under the player this might not be required.
            // localPlayer.SetVelocity(Vector3.zero);
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
