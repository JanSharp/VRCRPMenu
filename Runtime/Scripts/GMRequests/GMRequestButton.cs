using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        [Tooltip("The type of request to create when this button is clicked while turned off")]
        public GMRequestType primaryRequestType;
        public GMRequestType[] turnOnForRequestTypes;
        public Toggle toggle;
        public TextMeshProUGUI label;
        public string offText;
        public string onText;

        private bool ShouldBeOnForRequest(GMRequest request)
        {
            return request != null
                && (request.latencyRequestType == primaryRequestType
                    || System.Array.IndexOf(turnOnForRequestTypes, request.latencyRequestType) != -1);
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            UpdateToggleStateBasedOnLatest();
        }

        public void OnValueChanged()
        {
            bool isOn = toggle.isOn;
            label.text = isOn ? onText : offText;

            if (isOn && Networking.LocalPlayer.IsUserInVR())
                menuManager.IsMenuOpen = false;

            GMRequest latestRequest = requestsManager.GetLatestActiveLocalRequest();

            if (isOn == ShouldBeOnForRequest(latestRequest))
                return;

            if (!isOn)
            {
                requestsManager.SendDeleteIA(latestRequest);
                return;
            }

            if (latestRequest == null)
            {
                requestsManager.SendCreateIA(primaryRequestType);
                return;
            }

            requestsManager.SendSetRequestTypeIA(latestRequest, primaryRequestType);
        }

        private void UpdateToggleStateBasedOnLatest()
        {
            bool isOn = ShouldBeOnForRequest(requestsManager.GetLatestActiveLocalRequest());
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
