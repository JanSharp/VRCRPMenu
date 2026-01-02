using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestButton : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;

        public GMRequestType requestType;
        public Toggle toggle;
        public TextMeshProUGUI label;
        public string offText;
        public string onText;

        [PermissionDefinitionReference(nameof(associatedRequestPermissionDef))]
        public string associatedRequestPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition associatedRequestPermissionDef;

        private bool RequestMatchesButtonType(GMRequest request)
        {
            return request != null && request.latencyRequestType == requestType;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            UpdateToggleStateBasedOnLatest();
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            // TODO: Delete all active local requests.
            if (!associatedRequestPermissionDef.valueForLocalPlayer)
                toggle.isOn = false;
        }

        public void OnValueChanged()
        {
            bool isOn = toggle.isOn;
            label.text = isOn ? onText : offText;

            GMRequest latestRequest = requestsManager.GetLatestActiveLocalRequest();

            if (isOn == RequestMatchesButtonType(latestRequest))
                return;

            if (!isOn)
            {
                requestsManager.SendDeleteIA(latestRequest);
                return;
            }

            if (latestRequest == null)
            {
                requestsManager.SendCreateIA(requestType);
                return;
            }

            requestsManager.SendSetRequestTypeIA(latestRequest, requestType);
        }

        private void UpdateToggleStateBasedOnLatest()
        {
            bool isOn = RequestMatchesButtonType(requestsManager.GetLatestActiveLocalRequest());
            toggle.SetIsOnWithoutNotify(isOn);
            label.text = isOn ? onText : offText;
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreatedInLatency)]
        public void OnGMRequestCreatedInLatency() => UpdateToggleStateBasedOnLatest();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreated)]
        public void OnGMRequestCreated() => UpdateToggleStateBasedOnLatest(); // requestedAtTick is now known.
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency() => UpdateToggleStateBasedOnLatest();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency() => UpdateToggleStateBasedOnLatest();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency() => UpdateToggleStateBasedOnLatest();
    }
}
