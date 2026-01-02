using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestsHUD : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;
        [HideInInspector][SerializeField][SingletonReference] private HUDManagerAPI hudManager;

        public Transform requesterHUDRoot;
        public GameObject requesterRegularRoot;
        public GameObject requesterUrgentRoot;
        public Image requesterRegularImage;
        public Image requesterUrgentImage;
        private Image requesterCurrentImage;
        private bool requesterHUDIsShown = false;
        [Space]
        public Transform responderHUDRoot;
        public GameObject responderRegularRoot;
        public GameObject responderUrgentRoot;
        public TextMeshProUGUI responderCountText;
        private bool responderHUDIsShown = false;

        [Space]
        [PermissionDefinitionReference(nameof(requestGMPermissionDef))]
        public string requestGMPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMPermissionDef;

        [PermissionDefinitionReference(nameof(requestGMUrgentlyPermissionDef))]
        public string requestGMUrgentlyPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMUrgentlyPermissionDef;

        [PermissionDefinitionReference(nameof(viewAndEditGMRequestsPermissionDef))]
        public string viewAndEditGMRequestsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewAndEditGMRequestsPermissionDef;

        private bool cannotRequestRegular = false;
        private bool cannotRequestUrgent = false;
        private bool cannotViewAndEdit = false;

        private void Start()
        {
            hudManager.AddHUDElement(requesterHUDRoot, "ec[gm-requests]-e[requester]", isShown: false);
            hudManager.AddHUDElement(responderHUDRoot, "ec[gm-requests]-c[responder]", isShown: false);
        }

        private void ShowHideRequesterHUD(bool show)
        {
            if (show == requesterHUDIsShown)
                return;
            requesterHUDIsShown = show;
            if (requesterHUDIsShown)
                hudManager.ShowHUDElement(requesterHUDRoot);
            else
                hudManager.HideHUDElement(requesterHUDRoot);
        }

        private void ShowHideResponderHUD(bool show)
        {
            if (show == responderHUDIsShown)
                return;
            responderHUDIsShown = show;
            if (responderHUDIsShown)
                hudManager.ShowHUDElement(responderHUDRoot);
            else
                hudManager.HideHUDElement(responderHUDRoot);
        }

        private void UpdateRequesterHUD()
        {
            if (cannotRequestRegular && cannotRequestUrgent)
            {
                ShowHideRequesterHUD(false);
                return;
            }

            GMRequest request = requestsManager.GetLatestActiveLocalRequest();
            if (request == null)
            {
                // TODO: If there is now no active request and the previously active request has been marked
                // as read - not deleted - delay hiding the HUD for a total of 3 seconds and flash for 1 second
                // at the end too
                ShowHideRequesterHUD(false);
                return;
            }

            bool isRegular = request.latencyRequestType == GMRequestType.Regular;
            if (isRegular ? cannotRequestRegular : cannotRequestUrgent)
            {
                ShowHideRequesterHUD(false);
                return;
            }

            requesterCurrentImage = isRegular
                ? requesterRegularImage
                : requesterUrgentImage;
            requesterRegularRoot.SetActive(isRegular);
            requesterUrgentRoot.SetActive(!isRegular);

            ShowHideRequesterHUD(true);
        }

        private void UpdateResponderHUD()
        {
            if (cannotViewAndEdit)
            {
                ShowHideResponderHUD(false);
                return;
            }

            int count = requestsManager.ActiveRequestsCount;
            if (count == 0)
            {
                ShowHideResponderHUD(false);
                return;
            }

            bool anyUrgent = false;
            GMRequest[] requests = requestsManager.ActiveRequestsRaw;
            for (int i = 0; i < count; i++)
                if (requests[i].latencyRequestType == GMRequestType.Urgent)
                {
                    anyUrgent = true;
                    break;
                }

            responderCountText.text = count.ToString();
            responderRegularRoot.SetActive(!anyUrgent);
            responderUrgentRoot.SetActive(anyUrgent);
            // TODO: Show "regular presented as urgent" as its own icon too.

            ShowHideResponderHUD(true);
        }

        private void UpdateHUD()
        {
            UpdateRequesterHUD();
            UpdateResponderHUD();
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            cannotRequestRegular = !requestGMPermissionDef.valueForLocalPlayer;
            cannotRequestUrgent = !requestGMUrgentlyPermissionDef.valueForLocalPlayer;
            cannotViewAndEdit = !viewAndEditGMRequestsPermissionDef.valueForLocalPlayer;
            UpdateHUD();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreatedInLatency)]
        public void OnGMRequestCreatedInLatency() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency() => UpdateHUD();
    }
}
