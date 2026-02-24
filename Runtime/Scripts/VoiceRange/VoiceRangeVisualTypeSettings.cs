using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeVisualTypeSettings : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;

        [SerializeField] private VoiceRangeSettingsScriptType scriptType;
        [SerializeField] private VoiceRangeVisualTypeSettingToggle[] toggles;

        private void MakeTogglesMatchLatencyState()
        {
            VoiceRangePlayerData localPlayer = voiceRangeManager.LocalPlayer;
            VoiceRangeVisualizationType visualType = scriptType == VoiceRangeSettingsScriptType.InWorld
                ? localPlayer.latencyWorldVisualType
                : localPlayer.latencyHUDVisualType;
            foreach (VoiceRangeVisualTypeSettingToggle toggle in toggles)
                toggle.toggle.SetIsOnWithoutNotify(toggle.visualType == visualType);
        }

        public void OnValueChanged(VoiceRangeVisualTypeSettingToggle toggle)
        {
            if (!toggle.toggle.isOn)
            {
                toggle.toggle.SetIsOnWithoutNotify(true);
                return;
            }
            VoiceRangePlayerData localPlayer = voiceRangeManager.LocalPlayer;
            if (scriptType == VoiceRangeSettingsScriptType.InWorld)
                voiceRangeManager.SendSetInWorldSettingsIA(localPlayer, localPlayer.latencyShowInWorldMask, toggle.visualType);
            else
                voiceRangeManager.SendSetInHUDSettingsIA(localPlayer, localPlayer.latencyShowInHUDMask, toggle.visualType);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeTogglesMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeTogglesMatchLatencyState();

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
