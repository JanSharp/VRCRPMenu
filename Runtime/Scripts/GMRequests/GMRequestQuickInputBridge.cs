using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestQuickInputBridge : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;
        [HideInInspector][SerializeField][SingletonReference] private GMRequestQuickInputManagerAPI requestQuickInputManager;

        private bool shouldIgnoreInput;

        [PermissionDefinitionReference(nameof(requestGMPDef))]
        public string requestGMPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMPDef;

        [PermissionDefinitionReference(nameof(requestGMUrgentlyPDef))]
        public string requestGMUrgentlyPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMUrgentlyPDef;

        private void UpdateShouldIgnoreInput()
        {
            bool newShouldIgnore;
            GMRequest activeLocalRequest = requestsManager.GetLatestActiveLocalRequest();
            if (activeLocalRequest == null)
                newShouldIgnore = !requestGMPDef.valueForLocalPlayer; // Even if the player has permissions for urgent, do not let them in this manner.
            else
                newShouldIgnore = activeLocalRequest.latencyRequestType == GMRequestType.Regular
                    ? !requestGMPDef.valueForLocalPlayer
                    : !requestGMUrgentlyPDef.valueForLocalPlayer;
            if (shouldIgnoreInput == newShouldIgnore)
                return;
            shouldIgnoreInput = newShouldIgnore;
            if (shouldIgnoreInput)
                requestQuickInputManager.IncrementIgnoreInput();
            else
                requestQuickInputManager.DecrementIgnoreInput();
        }

        public override void InitializeInstantiated() { }

        public override void Resolve() => UpdateShouldIgnoreInput();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateShouldIgnoreInput();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreatedInLatency)]
        public void OnGMRequestCreatedInLatency() => UpdateShouldIgnoreInput();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency() => UpdateShouldIgnoreInput();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency() => UpdateShouldIgnoreInput();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency() => UpdateShouldIgnoreInput();

        [GMRequestsQuickInputEvent(GMRequestsQuickInputEventType.OnGMRequestQuickInputCompleted)]
        public void OnGMRequestQuickInputCompleted()
        {
            GMRequest latestRequest = requestsManager.GetLatestActiveLocalRequest();
            if (latestRequest == null)
                requestsManager.SendCreateIA(GMRequestType.Regular);
            else
                requestsManager.SendDeleteIA(latestRequest); // Delete regardless of request type.
        }
    }
}
