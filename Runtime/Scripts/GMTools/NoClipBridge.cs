using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipBridge : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;
        [HideInInspector][SerializeField][SingletonReference] private NoClipMovementAPI noClipMovement;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        private bool isMenuOpen;

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => UpdateSettings();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateSettings();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipSpeedChanged)]
        public void OnLocalLatencyNoClipSpeedChanged() => UpdateSpeed();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipFlyingTypeChanged)]
        public void OnLocalLatencyNoClipFlyingTypeChanged() => UpdateFlyingType();

        private void UpdateSettings()
        {
            UpdateSpeed();
            UpdateFlyingType();
        }

        private void UpdateSpeed()
        {
            noClipMovement.Speed = noClipSettingsManager.LatencyNoClipSpeed;
        }

        private void UpdateFlyingType()
        {
            NoClipFlyingType flyingType = noClipSettingsManager.LatencyNoClipFlyingType;
            noClipMovement.ModeWhileMoving = flyingType == NoClipFlyingType.Flying ? NoClipModeWhileMoving.Velocity
                : flyingType == NoClipFlyingType.NoClip ? NoClipModeWhileMoving.Teleport
                : NoClipModeWhileMoving.Velocity; // Fallback for any new NoClipFlyingType.
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuOpenStateChanged)]
        public void OnMenuOpenStateChanged()
        {
            // Even though this even only gets raised when the value changed, we cannot trust order of events.
            // Well in Udon we kind of can because if this were to recurse it would break horribly anyway, so
            // we can expect that there is no recursion which could cause such odd order of events. But still
            // in order to handle normal event based systems, handling those edge cases anyway.
            if (menuManager.IsMenuOpen == isMenuOpen)
                return;
            isMenuOpen = !isMenuOpen;
            // Force not teleporting the player while the menu is open while standing still,
            // otherwise it jumps around like crazy and is un-interactable.
            if (isMenuOpen)
                noClipMovement.IncrementAvoidTeleporting();
            else
                noClipMovement.DecrementAvoidTeleporting();
        }
    }
}
