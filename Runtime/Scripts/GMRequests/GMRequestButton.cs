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

        private GMRequest activeLocalRequest;
        private bool ActiveRequestMatchesType => activeLocalRequest != null
            && activeLocalRequest.latencyRequestType == requestType;

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            int count = requestsManager.GMRequestsCount;
            GMRequest[] requests = requestsManager.GMRequests;
            for (int i = 0; i < count; i++)
            {
                GMRequest request = requests[i];
                if (IsRelevantActiveLocalRequest(request))
                {
                    SetActiveLocalRequest(request);
                    return;
                }
            }
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (!associatedRequestPermissionDef.valueForLocalPlayer)
                toggle.isOn = false;
        }

        public void OnValueChanged()
        {
            bool isOn = toggle.isOn;
            label.text = isOn ? onText : offText;

            if (isOn == ActiveRequestMatchesType)
                return;

            if (!isOn)
            {
                requestsManager.SendDeleteIA(activeLocalRequest);
                return;
            }

            if (activeLocalRequest == null)
            {
                requestsManager.SendCreateIA(requestType);
                return;
            }

            requestsManager.SendSetRequestTypeIA(activeLocalRequest, requestType);
        }

        private void SetActiveLocalRequest(GMRequest request)
        {
            activeLocalRequest = request;
            bool isOn = ActiveRequestMatchesType;
            toggle.SetIsOnWithoutNotify(isOn);
            label.text = isOn ? onText : offText;
        }

        private void ClearActiveLocalRequest()
        {
            activeLocalRequest = null;
            toggle.SetIsOnWithoutNotify(false);
            label.text = offText;
        }

        private bool IsRelevantActiveLocalRequest(GMRequest request)
        {
            return request.requestingPlayer != null
                && request.requestingPlayer.core.isLocal
                && !request.latencyIsRead
                && !request.latencyIsDeleted;
        }

        private void OnGMRequestsEvent()
        {
            GMRequest request = requestsManager.RequestForEvent;
            if (IsRelevantActiveLocalRequest(request))
                SetActiveLocalRequest(request);
            else
                ClearActiveLocalRequest();
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreatedInLatency)]
        public void OnGMRequestCreatedInLatency() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreated)]
        public void OnGMRequestCreated() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChanged)]
        public void OnGMRequestChanged() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeleted)]
        public void OnGMRequestDeleted() => OnGMRequestsEvent();
    }
}
