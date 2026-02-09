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
        private float requesterElapsedTimeInAnimation;
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
        public Transform responderScaleRoot;
        [UIStyleColor(nameof(responderRegularColor))]
        public string responderRegularColorName;
        public Color responderRegularColor;
        [UIStyleColor(nameof(responderManyRegularColor))]
        public string responderManyRegularColorName;
        public Color responderManyRegularColor;
        [UIStyleColor(nameof(responderUrgentColor))]
        public string responderUrgentColorName;
        public Color responderUrgentColor;
        private const int ResponderManyRegularThreshold = 5;
        private const float ResponderRegularImageCrossFadeDuration = 0.2f;
        private bool responderHUDIsShown = false;
        private bool responderIsInNewRequestAnimation = false;
        private float responderFractionWithinNewRequestAnimation;
        private const float ResponderNewRequestTotalTime = 0.5f;
        private const float ResponderNewRequestScaleIncrease = 0.25f;
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
            hudManager.AddHUDElement(requesterHUDRoot, requesterHUDOrder, isShown: false);
            hudManager.AddHUDElement(responderHUDRoot, responderHUDOrder, isShown: false);
        }

        #region Requester

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
            SetRequesterIsInFadeOutAnimation(true);

            bool isRegular = prevActiveLocalRequest.latencyRequestType == GMRequestType.Regular;
            requesterCurrentImage = isRegular
                ? requesterRegularImage
                : requesterUrgentImage;
            requesterCurrentBaseColor = isRegular
                ? requesterRegularBaseColor
                : requesterUrgentBaseColor;
            requesterCurrentPulseColor = requesterCurrentBaseColor;
            requesterCurrentPulseColor.a *= 0.1f;
            requesterElapsedTimeInAnimation = 0f;
            updateManager.Register(this);
        }

        private void StopFadeOutAnimation()
        {
            if (!requesterIsInFadeOutAnimation)
                return;
#if RP_MENU_DEBUG
            Debug.Log($"[RPMenuDebug] GMRequestsHUD  StopFadeOutAnimation (inner) - stopping fade out animation");
#endif
            SetRequesterIsInFadeOutAnimation(false);
            requesterCurrentImage.color = requesterCurrentBaseColor;
            prevActiveLocalRequest = null;
            updateManager.Deregister(this);
        }

        private void SetRequesterIsInFadeOutAnimation(bool value)
        {
            requesterIsInFadeOutAnimation = value;
            UpdateUpdateManagerRegistration();
        }

        private void UpdateRequesterFadeOutAnimation()
        {
            requesterElapsedTimeInAnimation += Time.deltaTime;
            if (requesterElapsedTimeInAnimation >= RequesterFadeOutTotalTime)
            {
                ShowHideRequesterHUD(false);
                return;
            }
            if (requesterElapsedTimeInAnimation < RequesterFadeOutPulseStartTime)
                return;
            float timeInPulses = requesterElapsedTimeInAnimation - RequesterFadeOutPulseStartTime;
            float t = (Mathf.Cos(timeInPulses * RequesterFadeOutPulsesPerSecondWithTAU) + 1f) / 2f;
            // t is 1 when timeInAnimation is 0.
            requesterCurrentImage.color = Color.Lerp(requesterCurrentPulseColor, requesterCurrentBaseColor, t);
        }

        #endregion

        #region Responder

        private void ShowHideResponderHUD(bool show)
        {
            if (show == responderHUDIsShown)
                return;
            responderHUDIsShown = show;
            if (responderHUDIsShown)
                hudManager.ShowHUDElement(responderHUDRoot);
            else
            {
                hudManager.HideHUDElement(responderHUDRoot);
                StopResponderNewRequestAnimation();
            }
        }

        private bool HasManyActiveRequests()
        {
            return requestsManager.ActiveRequestsCount >= ResponderManyRegularThreshold;
        }

        private void UpdateResponderHUD(bool hasNewRequest)
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
            // Important to be called after SetActive to prevent instantly cancelling a cross fade.
            // And important to be called before ShowHideResponderHUD to use the correct fade duration.
            UpdateResponderImageColor(anyPresentAsUrgent);

            if (hasNewRequest)
                StartResponderNewRequestAnimation();
            ShowHideResponderHUD(true);
        }

        private void UpdateResponderImageColor(bool presentAsUrgent)
        {
            responderRegularImage.CrossFadeColor(
                presentAsUrgent ? responderUrgentColor
                    : HasManyActiveRequests() ? responderManyRegularColor
                    : responderRegularColor,
                responderHUDIsShown ? ResponderRegularImageCrossFadeDuration : 0f,
                ignoreTimeScale: true,
                useAlpha: true);
        }

        private void StartResponderNewRequestAnimation()
        {
            if (!responderIsInNewRequestAnimation)
                responderFractionWithinNewRequestAnimation = 0f;
            else if (responderFractionWithinNewRequestAnimation > 0.5f) // Mirror around 0.5f, basically.
                responderFractionWithinNewRequestAnimation = 1f - responderFractionWithinNewRequestAnimation;
            SetResponderIsInNewRequestAnimation(true);
        }

        private void StopResponderNewRequestAnimation()
        {
            if (!responderIsInNewRequestAnimation)
                return;
            SetResponderIsInNewRequestAnimation(false);
            responderScaleRoot.localScale = Vector3.one;
        }

        private void SetResponderIsInNewRequestAnimation(bool value)
        {
            responderIsInNewRequestAnimation = value;
            UpdateUpdateManagerRegistration();
        }

        private void UpdateResponderNewRequestAnimation()
        {
            responderFractionWithinNewRequestAnimation += Time.deltaTime / ResponderNewRequestTotalTime;
            if (responderFractionWithinNewRequestAnimation >= 1f)
            {
                StopResponderNewRequestAnimation();
                return;
            }
            float scale = 1f + Mathf.Sin(responderFractionWithinNewRequestAnimation * Mathf.PI) * ResponderNewRequestScaleIncrease;
            responderScaleRoot.localScale = Vector3.one * scale;
        }

        #endregion

        private void UpdateUpdateManagerRegistration()
        {
            if (requesterIsInFadeOutAnimation || responderIsInNewRequestAnimation)
                updateManager.Register(this);
            else
                updateManager.Deregister(this);
        }

        public void CustomUpdate()
        {
            if (requesterIsInFadeOutAnimation)
                UpdateRequesterFadeOutAnimation();
            if (responderIsInNewRequestAnimation)
                UpdateResponderNewRequestAnimation();
        }

        private void UpdateHUD(bool hasNewRequest = false)
        {
            UpdateRequesterHUD();
            UpdateResponderHUD(hasNewRequest);
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
        public void OnGMRequestCreatedInLatency() => UpdateHUD(hasNewRequest: true);

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestShouldPresetAsUrgentChanged)]
        public void OnGMRequestShouldPresetAsUrgentChanged()
        {
            GMRequest request = requestsManager.RequestForEvent;
            if (!request.latencyIsRead && request.latencyRequestType == GMRequestType.Regular)
                UpdateResponderImageColor(presentAsUrgent: true);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency() => UpdateHUD(); // Changing the request type does not count as a "new request".

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency() => UpdateHUD();

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency() => UpdateHUD();
    }
}
