using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    public enum VoiceRangeSettingsScriptType
    {
        InWorld,
        InHUD,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeShowSettings : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;

        [SerializeField] private VoiceRangeSettingsScriptType scriptType;
        [SerializeField] private VoiceRangeShowSettingToggle[] toggles;
        private uint defaultMaskForDefsWithoutToggles;

        private void Initialize()
        {
            defaultMaskForDefsWithoutToggles = scriptType == VoiceRangeSettingsScriptType.InWorld
                ? voiceRangeManager.DefaultShowInWorldMask
                : voiceRangeManager.DefaultShowInHUDMask;
            foreach (VoiceRangeShowSettingToggle toggle in toggles)
            {
                VoiceRangeDefinition def = voiceRangeManager.GetVoiceRangeDefinition(toggle.voiceRangeInternalName);
                toggle.resolvedDef = def;
                defaultMaskForDefsWithoutToggles &= ~def.bitMaskFlag;
            }
            MakeTogglesMatchLatencyState();
        }

        private void MakeTogglesMatchLatencyState()
        {
            VoiceRangePlayerData localPlayer = voiceRangeManager.LocalPlayer;
            uint showMask = scriptType == VoiceRangeSettingsScriptType.InWorld
                ? localPlayer.latencyShowInWorldMask
                : localPlayer.latencyShowInHUDMask;
            foreach (VoiceRangeShowSettingToggle toggle in toggles)
                toggle.toggle.SetIsOnWithoutNotify((showMask & toggle.resolvedDef.bitMaskFlag) != 0u);
        }

        public void OnValueChanged()
        {
            uint showMask = defaultMaskForDefsWithoutToggles;
            foreach (VoiceRangeShowSettingToggle toggle in toggles)
                if (toggle.toggle.isOn)
                    showMask |= toggle.resolvedDef.bitMaskFlag;
            VoiceRangePlayerData localPlayer = voiceRangeManager.LocalPlayer;
            if (scriptType == VoiceRangeSettingsScriptType.InWorld)
                voiceRangeManager.SendSetInWorldSettingsIA(localPlayer, showMask, localPlayer.latencyWorldVisualType);
            else
                voiceRangeManager.SendSetInHUDSettingsIA(localPlayer, showMask, localPlayer.latencyHUDVisualType);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => Initialize();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => Initialize();

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp() => MakeTogglesMatchLatencyState();

        [VoiceRangeEvent(VoiceRangeEventType.OnLocalInWorldSettingsChangedInLatency)]
        public void OnLocalInWorldSettingsChangedInLatency()
        {
            if (scriptType == VoiceRangeSettingsScriptType.InWorld)
                MakeTogglesMatchLatencyState();
        }

        [VoiceRangeEvent(VoiceRangeEventType.OnLocalInHUDSettingsChangedInLatency)]
        public void OnLocalInHUDSettingsChangedInLatency()
        {
            if (scriptType == VoiceRangeSettingsScriptType.InHUD)
                MakeTogglesMatchLatencyState();
        }
    }
}
