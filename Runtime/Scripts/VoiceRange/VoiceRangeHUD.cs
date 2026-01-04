using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeHUD : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;
        [HideInInspector][SerializeField][SingletonReference] private HUDManagerAPI hudManager;

        [SerializeField] private Transform hudRoot;
        [SerializeField] private VoiceRangeHUDIcon[] icons;
        /// <summary>
        /// <para>Matches the <see cref="VoiceRangeDefinition.index"/>.</para>
        /// <para>Can contain <see langword="null"/>.</para>
        /// </summary>
        private VoiceRangeHUDIcon[] sortedIcons;
        private VoiceRangeHUDIcon activeIcon;
        private float blinksPerSecondWithTAU;

        private const float TAU = Mathf.PI * 2f;
        private const float PulseBlinksPerSecondWithTAU = 0.4f * TAU;
        private const float BlinkBlinksPerSecondWithTAU = 2.5f * TAU;

        [PermissionDefinitionReference(nameof(voiceRangeHUDSettingsPermissionDef))]
        public string voiceRangeHUDSettingsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition voiceRangeHUDSettingsPermissionDef;

        private int shownCounter = 0;

        private bool isInitialized = false;
        private VoiceRangePlayerData localPlayer;

        private void Start()
        {
            hudManager.AddHUDElement(hudRoot, "oc[voice-range]", isShown: false);
        }

        private void Initialize()
        {
            localPlayer = voiceRangeManager.LocalPlayer;
            sortedIcons = new VoiceRangeHUDIcon[voiceRangeManager.VoiceRangeDefinitionCount];
            foreach (VoiceRangeHUDIcon icon in icons)
            {
                icon.hudController = this;
                VoiceRangeDefinition def = voiceRangeManager.GetVoiceRangeDefinition(icon.voiceRangeInternalName);
                sortedIcons[def.index] = icon;
                icon.resolvedDef = def;
                Color color = icon.iconImage.color * def.color;
                icon.maxColor = color;
                color.a *= 0.5f;
                icon.minColor = color;
                color.a = 0;
                icon.offColor = color;
                icon.iconGo.SetActive(false);
                icon.iconImage.color = color;
            }
            isInitialized = true;
            UpdateToMatchLatencyState();
        }

        public void IncrementShown()
        {
            if (++shownCounter == 1)
                hudManager.ShowHUDElement(hudRoot);
        }

        public void DecrementShown()
        {
            if (--shownCounter == 0)
                hudManager.HideHUDElement(hudRoot);
        }

        private void UpdateToMatchLatencyState()
        {
            UpdateBlinksPerSecond();
            UpdateActiveIcon();
        }

        private void UpdateBlinksPerSecond()
        {
            if (!isInitialized)
                return;
            VoiceRangeVisualizationType visualType = voiceRangeHUDSettingsPermissionDef.valueForLocalPlayer
                ? localPlayer.latencyHUDVisualType
                : VoiceRangeVisualizationType.Default;
            switch (visualType)
            {
                case VoiceRangeVisualizationType.Pulse:
                    blinksPerSecondWithTAU = PulseBlinksPerSecondWithTAU;
                    break;
                case VoiceRangeVisualizationType.Blink:
                    blinksPerSecondWithTAU = BlinkBlinksPerSecondWithTAU;
                    break;
                default:
                    blinksPerSecondWithTAU = 0f;
                    break;
            }
            if (activeIcon != null)
                activeIcon.SetBlinksPerSecond(blinksPerSecondWithTAU);
        }

        private void UpdateActiveIcon()
        {
            if (!isInitialized)
                return;
            int index = localPlayer.latencyVoiceRangeIndex;
            uint showMask = voiceRangeHUDSettingsPermissionDef.valueForLocalPlayer
                ? localPlayer.latencyShowInHUDMask
                : voiceRangeManager.DefaultShowInHUDMask;
            VoiceRangeDefinition def = voiceRangeManager.GetVoiceRangeDefinition(index);
            VoiceRangeHUDIcon nextActiveIcon = (showMask & def.bitMaskFlag) == 0u
                ? null
                : sortedIcons[index];

            if (activeIcon == nextActiveIcon)
                return;
            if (activeIcon != null)
                activeIcon.FadeOut();
            if (nextActiveIcon != null)
            {
                nextActiveIcon.SetBlinksPerSecond(blinksPerSecondWithTAU);
                nextActiveIcon.FadeIn();
            }
            activeIcon = nextActiveIcon;
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
        public void OnLocalVoiceRangeIndexChangedInLatency() => UpdateActiveIcon();

        [VoiceRangeEvent(VoiceRangeEventType.OnLocalInHUDSettingsChangedInLatency)]
        public void OnLocalInHUDSettingsChangedInLatency() => UpdateToMatchLatencyState();
    }
}
