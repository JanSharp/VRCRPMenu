using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeInWorldIndicator : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;
        [HideInInspector][SerializeField][SingletonReference] private HUDManagerAPI hudManager;

        private bool isInitialized = false;
        private VoiceRangePlayerData localPlayer;

        private void Initialize()
        {
            // localPlayer = voiceRangeManager.LocalPlayer;
            // sortedIcons = new VoiceRangeHUDIcon[voiceRangeManager.VoiceRangeDefinitionCount];
            // foreach (VoiceRangeHUDIcon icon in icons)
            // {
            //     icon.hudController = this;
            //     VoiceRangeDefinition def = voiceRangeManager.GetVoiceRangeDefinition(icon.voiceRangeInternalName);
            //     sortedIcons[def.index] = icon;
            //     icon.resolvedDef = def;
            //     Color color = icon.iconImage.color * def.color;
            //     icon.maxColor = color;
            //     color.a *= 0.5f;
            //     icon.minColor = color;
            //     color.a = 0;
            //     icon.offColor = color;
            //     icon.iconGo.SetActive(false);
            //     icon.iconImage.color = color;
            // }
            // isInitialized = true;
            // UpdateToMatchLatencyState();
        }

        private void UpdateToMatchLatencyState()
        {
            // UpdateBlinksPerSecond();
            // UpdateActiveIcon();
        }

        public override void InitializeInstantiated() { }

        public override void Resolve() => UpdateToMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => Initialize();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => Initialize();

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp() => UpdateToMatchLatencyState();

        // [VoiceRangeEvent(VoiceRangeEventType.OnLocalVoiceRangeIndexChangedInLatency)]
        // public void OnLocalVoiceRangeIndexChangedInLatency() => UpdateActiveIcon();

        [VoiceRangeEvent(VoiceRangeEventType.OnLocalInHUDSettingsChangedInLatency)]
        public void OnLocalInHUDSettingsChangedInLatency() => UpdateToMatchLatencyState();
    }
}
