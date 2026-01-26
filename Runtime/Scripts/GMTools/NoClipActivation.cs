using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipActivation : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipManagerAPI noClipManager;

        private const float DoubleClickInterval = 0.4f;

        private bool noClipEnabled;
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
            noClipManager.IsNoClipActive = !noClipManager.IsNoClipActive;
        }

        private void UpdateNoClipEnabled()
        {
            noClipEnabled = noClipManager.LatencyNoClipEnabled;
            if (!noClipEnabled)
                lastJumpInputTime = -1f; // For extra cleanliness.
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => UpdateNoClipEnabled();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateNoClipEnabled();

        [NoClipEvent(NoClipEventType.OnLocalLatencyNoClipEnabledChanged)]
        public void OnLocalLatencyNoClipEnabledChanged() => UpdateNoClipEnabled();
    }
}
