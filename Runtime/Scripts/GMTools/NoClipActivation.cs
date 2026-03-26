using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipActivation : PermissionResolver
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
        private bool hasAnyPermission = false;
        private float lastJumpInputTime = -1f;

        [PermissionDefinitionReference(nameof(useFlyingPDef))]
        public string useFlyingPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition useFlyingPDef;

        [PermissionDefinitionReference(nameof(useNoClipPDef))]
        public string useNoClipPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition useNoClipPDef;

        public override void InitializeInstantiated() { }

        public override void ResolveAll()
        {
            // When losing only one permission, the flying type will
            // get changed to the other one and it should stay active.
            hasAnyPermission = useFlyingPDef.valueForLocalPlayer || useNoClipPDef.valueForLocalPlayer;
            if (!hasAnyPermission)
                ForceDeactivate();
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value || !noClipEnabled || !hasAnyPermission)
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
                ForceDeactivate();
        }

        private void ForceDeactivate()
        {
            noClipMovement.IsNoClipActive = false;
            lastJumpInputTime = -1f; // For extra cleanliness.
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => UpdateNoClipEnabled();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateNoClipEnabled();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipEnabledChanged)]
        public void OnLocalLatencyNoClipEnabledChanged() => UpdateNoClipEnabled();
    }
}
