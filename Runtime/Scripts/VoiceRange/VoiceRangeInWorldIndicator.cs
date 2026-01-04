using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// NOTE: This file contains tons of copy paste from VoiceRangeHUD.cs and VoiceRangeHUDIcon.cs

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeInWorldIndicator : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;
        [HideInInspector][SerializeField][SingletonReference] private BoneAttachmentManager boneAttachmentManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        [SerializeField] private GameObject indicatorGo;
        [SerializeField] private Transform indicatorTransform;
        [SerializeField] private Renderer indicatorRenderer;
        private Material indicatorMaterial;
        private int innerRadiusPropId;
        private int middleRadiusPropId;
        private int outerRadiusPropId;

        [PermissionDefinitionReference(nameof(voiceRangeWorldSettingsPermissionDef))]
        public string voiceRangeWorldSettingsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition voiceRangeWorldSettingsPermissionDef;

        private Color[] minColors;
        private Color[] maxColors;
        private VoiceRangeDefinition activeDef;
        private Color activeMinColor;
        private Color activeMaxColor;
        private float activeRadius;

        private bool isInitialized = false;
        private VoiceRangePlayerData localPlayer;
        private const float InnerRadiusMultiplier = 0.97f;
        private const float OuterRadiusMultiplier = 1.25f;

        private bool isShown = false;
        private VoiceRangeHUDIconState indicatorState = VoiceRangeHUDIconState.Off;
        private VoiceRangeVisualizationType visualType = VoiceRangeVisualizationType.Static;

        private float fadeElapsedTime = 0f;
        private float blinkingElapsedTime = 0f;
        private Color fadeSourceColor;
        private Color fadeDestinationColor;
        private float shownRadius;
        private float fadeSourceRadius;
        private float fadeDestinationRadius;

        private void Start()
        {
            indicatorMaterial = indicatorRenderer.material; // Intentionally not using shared to not edit the asset.
            innerRadiusPropId = VRCShader.PropertyToID("_InnerRadius");
            middleRadiusPropId = VRCShader.PropertyToID("_MiddleRadius");
            outerRadiusPropId = VRCShader.PropertyToID("_OuterRadius");
        }

        private void Initialize()
        {
            isInitialized = true;
            localPlayer = voiceRangeManager.LocalPlayer;

            Color baseColor = indicatorMaterial.color;
            int count = voiceRangeManager.VoiceRangeDefinitionCount;
            minColors = new Color[count];
            maxColors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                VoiceRangeDefinition def = voiceRangeManager.GetVoiceRangeDefinition(i);
                Color color = baseColor * def.color;
                maxColors[i] = color;
                color.a *= 0.25f;
                minColors[i] = color;
            }

            VoiceRangeDefinition initialDef = voiceRangeManager.GetVoiceRangeDefinition(localPlayer.latencyVoiceRangeIndex);
            activeRadius = initialDef.farRange;
            SetShownRadius(activeRadius);
            baseColor *= initialDef.color;
            baseColor.a = 0f;
            indicatorMaterial.color = baseColor;

            UpdateToMatchLatencyState();
        }

        private void SetShownRadius(float radius)
        {
            shownRadius = radius;
            indicatorMaterial.SetFloat(innerRadiusPropId, shownRadius * InnerRadiusMultiplier);
            indicatorMaterial.SetFloat(middleRadiusPropId, shownRadius);
            indicatorMaterial.SetFloat(outerRadiusPropId, shownRadius * OuterRadiusMultiplier);
        }

        private void Show()
        {
            if (isShown)
                return;
            isShown = true;
            boneAttachmentManager.AttachToLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, indicatorTransform);
            indicatorTransform.localPosition = Vector3.zero;
            indicatorTransform.localRotation = Quaternion.identity;
            indicatorGo.SetActive(true);
        }

        private void Hide()
        {
            if (!isShown)
                return;
            isShown = false;
            boneAttachmentManager.DetachFromLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, indicatorTransform);
            indicatorGo.SetActive(false);
        }

        private void SetIconStateToStatic()
        {
            indicatorMaterial.color = activeMaxColor;
            visualType = VoiceRangeVisualizationType.Static;
            indicatorState = VoiceRangeHUDIconState.MatchingVisualType;
            updateManager.Deregister(this);
        }

        public void SetVisualType(VoiceRangeVisualizationType visualType)
        {
            if (this.visualType == visualType)
                return;
            if (indicatorState != VoiceRangeHUDIconState.MatchingVisualType)
            {
                this.visualType = visualType;
                return;
            }

            if (visualType == VoiceRangeVisualizationType.Static)
                SetIconStateToStatic();
            else
            {
                this.visualType = visualType;
                updateManager.Register(this);
            }
        }

        public void FadeIn()
        {
            fadeElapsedTime = 0;
            if (visualType != VoiceRangeVisualizationType.Pulse)
                blinkingElapsedTime = 0f;
            fadeSourceColor = indicatorMaterial.color;
            fadeDestinationColor = GetColorAtCurrentBlinkingElapsedTime();
            fadeSourceRadius = shownRadius;
            fadeDestinationRadius = activeRadius;
            indicatorState = VoiceRangeHUDIconState.FadingIn;
            updateManager.Register(this);
            Show();
        }

        private void FinishedFadingIn()
        {
            if (visualType == VoiceRangeVisualizationType.Static)
                SetIconStateToStatic(); // fadeDestinationColor may not match maxColor.
            else
                indicatorState = VoiceRangeHUDIconState.MatchingVisualType;
        }

        public void FadeOut()
        {
            if (indicatorState == VoiceRangeHUDIconState.Off || indicatorState == VoiceRangeHUDIconState.FadingOut)
                return;
            fadeElapsedTime = 0;
            fadeSourceColor = indicatorMaterial.color;
            fadeDestinationColor = fadeSourceColor;
            fadeDestinationColor.a = 0f;
            fadeSourceRadius = shownRadius;
            fadeDestinationRadius = activeRadius;
            indicatorState = VoiceRangeHUDIconState.FadingOut;
            updateManager.Register(this);
        }

        private void FinishedFadingOut()
        {
            blinkingElapsedTime = 0f;
            indicatorState = VoiceRangeHUDIconState.Off;
            updateManager.Deregister(this);
            Hide();
        }

        private Color GetColorAtCurrentBlinkingElapsedTime()
        {
            if (visualType != VoiceRangeVisualizationType.Pulse)
                return activeMaxColor;
            float t = (Mathf.Cos(blinkingElapsedTime * VoiceRangeHUDIcon.PulsesPerSecondWithTAU) + 1f) / 2f;
            // t is 1 when blinkingElapsedTime is 0.
            return Color.Lerp(activeMinColor, activeMaxColor, t);
        }

        public void CustomUpdate()
        {
            if (indicatorState == VoiceRangeHUDIconState.MatchingVisualType)
            {
                blinkingElapsedTime += Time.deltaTime;
                if (visualType == VoiceRangeVisualizationType.Pulse)
                {
                    // This is GetColorAtCurrentBlinkingElapsedTime inlined (aka copy pasted).
                    float t = (Mathf.Cos(blinkingElapsedTime * VoiceRangeHUDIcon.PulsesPerSecondWithTAU) + 1f) / 2f;
                    indicatorMaterial.color = Color.Lerp(activeMinColor, activeMaxColor, t);
                }
                else // Blink
                {
                    float t = (blinkingElapsedTime % VoiceRangeHUDIcon.BlinkInterval) / VoiceRangeHUDIcon.BlinkInterval;
                    indicatorMaterial.color = t < 0.5f ? activeMaxColor : activeMinColor;
                }
                return;
            }

            // Fading in or fading out.
            fadeElapsedTime += Time.deltaTime;
            if (fadeElapsedTime < VoiceRangeHUDIcon.FadeDuration)
            {
                float t = fadeElapsedTime / VoiceRangeHUDIcon.FadeDuration;
                t = (Mathf.Cos(t * Mathf.PI) + 1f) / 2f; // t ends up going from 1 to 0.
                indicatorMaterial.color = Color.Lerp(fadeDestinationColor, fadeSourceColor, t);
                SetShownRadius(Mathf.Lerp(fadeDestinationRadius, fadeSourceRadius, t));
            }
            else
            {
                indicatorMaterial.color = fadeDestinationColor;
                SetShownRadius(fadeDestinationRadius);
                if (indicatorState == VoiceRangeHUDIconState.FadingIn)
                    FinishedFadingIn();
                else
                    FinishedFadingOut();
            }
        }

        private void UpdateToMatchLatencyState()
        {
            UpdateVisualType();
            UpdateActiveVoiceRange();
        }

        private void UpdateVisualType()
        {
            if (!isInitialized)
                return;
            SetVisualType(voiceRangeWorldSettingsPermissionDef.valueForLocalPlayer
                ? localPlayer.latencyWorldVisualType
                : voiceRangeManager.DefaultWorldVisualType);
        }

        private void UpdateActiveVoiceRange()
        {
            if (!isInitialized)
                return;
            int index = localPlayer.latencyVoiceRangeIndex;
            uint showMask = voiceRangeWorldSettingsPermissionDef.valueForLocalPlayer
                ? localPlayer.latencyShowInWorldMask
                : voiceRangeManager.DefaultShowInWorldMask;
            VoiceRangeDefinition nextActiveDef = voiceRangeManager.GetVoiceRangeDefinition(index);

            activeRadius = nextActiveDef.farRange;
            if ((showMask & nextActiveDef.bitMaskFlag) == 0u)
                nextActiveDef = null;
            if (activeDef == nextActiveDef)
                return;
            activeDef = nextActiveDef;
            activeMinColor = minColors[index];
            activeMaxColor = maxColors[index];

            if (activeDef != null)
                FadeIn();
            else
                FadeOut();
        }

        public override void InitializeInstantiated() { }

        public override void Resolve() => UpdateToMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => Initialize();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => Initialize();

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp() => UpdateToMatchLatencyState();

        [VoiceRangeEvent(VoiceRangeEventType.OnLocalVoiceRangeIndexChangedInLatency)]
        public void OnLocalVoiceRangeIndexChangedInLatency() => UpdateActiveVoiceRange();

        [VoiceRangeEvent(VoiceRangeEventType.OnLocalInWorldSettingsChangedInLatency)]
        public void OnLocalInWorldSettingsChangedInLatency() => UpdateToMatchLatencyState();
    }
}
