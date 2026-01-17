using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestsHUD : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        [HideInInspector][SerializeField][SingletonReference] private HUDManagerAPI hudManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public Transform requesterHUDRoot;
        public GameObject requesterRegularRoot;
        public GameObject requesterUrgentRoot;
        public Image requesterRegularImage;
        public Image requesterUrgentImage;
        private Color requesterRegularBaseColor;
        private Color requesterUrgentBaseColor;
        private Color requesterCurrentBaseColor;
        private Color requesterCurrentPulseColor;
        private Image requesterCurrentImage;
        private bool requesterHUDIsShown = false;
        private GMRequest prevActiveLocalRequest = null;
        private bool requesterIsInFadeOutAnimation = false;
        private float elapsedTimeInAnimation;
        private const float RequesterFadeOutTotalTime = 3f;
        private const float RequesterFadeOutPulseStartTime = 2f;
        private const float RequesterFadeOutPulsesPerSecondWithTAU = 3.5f * TAU;
        private const float TAU = Mathf.PI * 2f;
        [Space]
        public Transform responderHUDRoot;
        public GameObject responderRegularRoot;
        public GameObject responderUrgentRoot;
        public Image responderRegularImage;
        public Image responderUrgentImage;
        public Text responderCountText;
        private Color responderRegularBaseColor;
        private Color responderUrgentBaseColor;
        private bool responderHUDIsShown = false;
        [Space]
        public string requesterHUDOrder = "im[gm-requests]-i[requester]";
        public string responderHUDOrder = "im[gm-requests]-e[responder]";

        [Space]
        [PermissionDefinitionReference(nameof(requestGMPDef))]
        public string requestGMPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMPDef;

        [PermissionDefinitionReference(nameof(requestGMUrgentlyPDef))]
        public string requestGMUrgentlyPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMUrgentlyPDef;

        [PermissionDefinitionReference(nameof(viewAndEditGMRequestsPDef))]
        public string viewAndEditGMRequestsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewAndEditGMRequestsPDef;

        private bool cannotRequestRegular = false;
        private bool cannotRequestUrgent = false;
        private bool cannotViewAndEdit = false;

        private void Start()
        {
            requesterRegularBaseColor = requesterRegularImage.color;
            requesterUrgentBaseColor = requesterUrgentImage.color;
            responderRegularBaseColor = responderRegularImage.color;
            responderUrgentBaseColor = responderUrgentImage.color;
            hudManager.AddHUDElement(requesterHUDRoot, requesterHUDOrder, isShown: false);
            hudManager.AddHUDElement(responderHUDRoot, responderHUDOrder, isShown: false);
        }

        private void ShowHideRequesterHUD(bool show)
        {
#if RP_MENU_DEBUG
            Debug.Log($"[RPMenuDebug] GMRequestsHUD  ShowHideRequesterHUD - show: {show}");
#endif
            if (show == requesterHUDIsShown)
                return;
            requesterHUDIsShown = show;
            if (requesterHUDIsShown)
                hudManager.ShowHUDElement(requesterHUDRoot);
            else
            {
                hudManager.HideHUDElement(requesterHUDRoot);
                StopFadeOutAnimation();
            }
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
                PotentiallyStartFadeOutAnimation();
                return;
            }

            bool isRegular = request.latencyRequestType == GMRequestType.Regular;
            if (isRegular ? cannotRequestRegular : cannotRequestUrgent)
            {
                ShowHideRequesterHUD(false);
                return;
            }

            StopFadeOutAnimation();
            prevActiveLocalRequest = request;
            requesterRegularRoot.SetActive(isRegular);
            requesterUrgentRoot.SetActive(!isRegular);

            ShowHideRequesterHUD(true);
        }

        private void PotentiallyStartFadeOutAnimation()
        {
#if RP_MENU_DEBUG
            Debug.Log($"[RPMenuDebug] GMRequestsHUD  PotentiallyStartFadeOutAnimation - prevActiveLocalRequest != null: {prevActiveLocalRequest != null}");
            if (prevActiveLocalRequest != null)
                Debug.Log($"[RPMenuDebug] GMRequestsHUD  PotentiallyStartFadeOutAnimation - prevActiveLocalRequest.latencyIsDeleted: {prevActiveLocalRequest.latencyIsDeleted}, prevActiveLocalRequest.latencyIsRead: {prevActiveLocalRequest.latencyIsRead}");
#endif
            if (prevActiveLocalRequest == null
                || prevActiveLocalRequest.latencyIsDeleted
                || !prevActiveLocalRequest.latencyIsRead)
            {
                ShowHideRequesterHUD(false);
                return;
            }
            if (requesterIsInFadeOutAnimation)
                return;
#if RP_MENU_DEBUG
            Debug.Log($"[RPMenuDebug] GMRequestsHUD  PotentiallyStartFadeOutAnimation (inner) - starting fade out animation");
#endif
            requesterIsInFadeOutAnimation = true;

            bool isRegular = prevActiveLocalRequest.latencyRequestType == GMRequestType.Regular;
            requesterCurrentImage = isRegular
                ? requesterRegularImage
                : requesterUrgentImage;
            requesterCurrentBaseColor = isRegular
                ? requesterRegularBaseColor
                : requesterUrgentBaseColor;
            requesterCurrentPulseColor = requesterCurrentBaseColor;
            requesterCurrentPulseColor.a *= 0.1f;
            elapsedTimeInAnimation = 0f;
            updateManager.Register(this);
        }

        private void StopFadeOutAnimation()
        {
            if (!requesterIsInFadeOutAnimation)
                return;
#if RP_MENU_DEBUG
            Debug.Log($"[RPMenuDebug] GMRequestsHUD  StopFadeOutAnimation (inner) - stopping fade out animation");
#endif
            requesterIsInFadeOutAnimation = false;
            requesterCurrentImage.color = requesterCurrentBaseColor;
            prevActiveLocalRequest = null;
            updateManager.Deregister(this);
        }

        public void CustomUpdate()
        {
            elapsedTimeInAnimation += Time.deltaTime;
            if (elapsedTimeInAnimation >= RequesterFadeOutTotalTime)
            {
                ShowHideRequesterHUD(false);
                return;
            }
            if (elapsedTimeInAnimation < RequesterFadeOutPulseStartTime)
                return;
            float timeInPulses = elapsedTimeInAnimation - RequesterFadeOutPulseStartTime;
            float t = (Mathf.Cos(timeInPulses * RequesterFadeOutPulsesPerSecondWithTAU) + 1f) / 2f;
            // t is 1 when timeInAnimation is 0.
            requesterCurrentImage.color = Color.Lerp(requesterCurrentPulseColor, requesterCurrentBaseColor, t);
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
            bool anyPresentAsUrgent = false;
            GMRequest[] requests = requestsManager.ActiveRequestsRaw;
            for (int i = 0; i < count; i++)
            {
                var request = requests[i];
                if (request.latencyRequestType == GMRequestType.Urgent)
                {
                    anyUrgent = true;
                    break;
                }
                if (!anyPresentAsUrgent && requestsManager.ShouldPresetAsUrgent(request))
                    anyPresentAsUrgent = true;
            }

            responderCountText.text = count.ToString();
            responderRegularRoot.SetActive(!anyUrgent);
            responderUrgentRoot.SetActive(anyUrgent);
            SetPresentAsUrgent(anyPresentAsUrgent);

            ShowHideResponderHUD(true);
        }

        private void SetPresentAsUrgent(bool presentAsUrgent)
        {
            responderRegularImage.color = presentAsUrgent
                ? responderUrgentBaseColor
                : responderRegularBaseColor;
        }

        private void UpdateHUD()
        {
            UpdateRequesterHUD();
            UpdateResponderHUD();
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            cannotRequestRegular = !requestGMPDef.valueForLocalPlayer;
            cannotRequestUrgent = !requestGMUrgentlyPDef.valueForLocalPlayer;
            cannotViewAndEdit = !viewAndEditGMRequestsPDef.valueForLocalPlayer;
            UpdateHUD();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreatedInLatency)]
        public void OnGMRequestCreatedInLatency() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestShouldPresetAsUrgentChanged)]
        public void OnGMRequestShouldPresetAsUrgentChanged()
        {
            GMRequest request = requestsManager.RequestForEvent;
            if (!request.latencyIsRead && request.latencyRequestType == GMRequestType.Regular)
                SetPresentAsUrgent(true);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency() => UpdateHUD();
    }
}
