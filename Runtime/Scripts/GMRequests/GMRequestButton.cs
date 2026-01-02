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

        private GMRequest[] activeLocalRequests = new GMRequest[ArrList.MinCapacity];
        private int activeLocalRequestsCount = 0;

        private GMRequest GetLatestActiveLocalRequest()
        {
            if (activeLocalRequestsCount == 0)
                return null;
            GMRequest result = activeLocalRequests[0];
            for (int i = 1; i < activeLocalRequestsCount; i++)
            {
                GMRequest request = activeLocalRequests[i];
                if (result.isLatency)
                {
                    if (request.isLatency && request.uniqueId > result.uniqueId)
                        result = request;
                    continue;
                }
                if (request.isLatency || request.requestedAtTick > result.requestedAtTick)
                    result = request;
            }
            return result;
        }

        private bool RequestMatchesButtonType(GMRequest request)
        {
            return request != null && request.latencyRequestType == requestType;
        }

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
                    AddActiveLocalRequest(request);
                    return;
                }
            }
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

            GMRequest latestRequest = GetLatestActiveLocalRequest();

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

        private void AddActiveLocalRequest(GMRequest request)
        {
            if (!ArrList.Contains(ref activeLocalRequests, ref activeLocalRequestsCount, request))
                ArrList.Add(ref activeLocalRequests, ref activeLocalRequestsCount, request);
            UpdateToggleStateBasedOnLatest();
        }

        private void RemoveActiveLocalRequest(GMRequest request)
        {
            if (ArrList.Remove(ref activeLocalRequests, ref activeLocalRequestsCount, request) == -1)
                return;
            UpdateToggleStateBasedOnLatest();
        }

        private void UpdateToggleStateBasedOnLatest()
        {
            bool isOn = RequestMatchesButtonType(GetLatestActiveLocalRequest());
            toggle.SetIsOnWithoutNotify(isOn);
            label.text = isOn ? onText : offText;
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
                AddActiveLocalRequest(request);
            else
                RemoveActiveLocalRequest(request);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreatedInLatency)]
        public void OnGMRequestCreatedInLatency() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreated)]
        public void OnGMRequestCreated() => OnGMRequestsEvent(); // requestedAtTick is now known.
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency() => OnGMRequestsEvent();
        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency() => OnGMRequestsEvent();
    }
}
