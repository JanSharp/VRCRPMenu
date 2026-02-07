using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipActivation : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;
        [HideInInspector][SerializeField][SingletonReference] private NoClipMovementAPI noClipMovement;

        private const float DoubleClickInterval = 0.4f;

        /// <summary>
        /// <para>Important for this to be <see langword="false"/> until
        /// <see cref="LockstepEventType.OnInit"/> or <see cref="LockstepEventType.OnClientBeginCatchUp"/>,
        /// because there is not event for the initial for when the initial value for
        /// <see cref="NoClipSettingsManagerAPI.LatencyNoClipEnabled"/> gets set. Or well those 2 lockstep
        /// events are the events for that, technically.</para>
        /// </summary>
        private bool noClipEnabled = false;
        private float lastJumpInputTime = -1f;

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value || !noClipEnabled)
                return;
            float time = Time.time;
            if (time > lastJumpInputTime + DoubleClickInterval)
                lastJumpInputTime = time;
            else
            {
                lastJumpInputTime = -1f;
                DidDoubleJump();
            }
        }

        private void DidDoubleJump()
        {
            noClipMovement.IsNoClipActive = !noClipMovement.IsNoClipActive;
        }

        private void UpdateNoClipEnabled()
        {
            noClipEnabled = noClipSettingsManager.LatencyNoClipEnabled;
            if (!noClipEnabled)
            {
                noClipMovement.IsNoClipActive = false;
                lastJumpInputTime = -1f; // For extra cleanliness.
            }
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => UpdateNoClipEnabled();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateNoClipEnabled();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipEnabledChanged)]
        public void OnLocalLatencyNoClipEnabledChanged() => UpdateNoClipEnabled();
    }
}
