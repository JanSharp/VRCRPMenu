using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeToggleGroup : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;

        public VoiceRangeToggle[] toggles;

        private void MakeTogglesMatchLatencyState()
        {
            int voiceRangeIndex = voiceRangeManager.LocalPlayer.latencyVoiceRangeIndex;
            string internalName = voiceRangeManager.GetVoiceRangeDefinition(voiceRangeIndex).internalName;
            foreach (VoiceRangeToggle toggle in toggles)
                toggle.toggle.SetIsOnWithoutNotify(toggle.voiceRangeInternalName == internalName);
        }

        public void OnValueChanged(VoiceRangeToggle toggle)
        {
            if (!toggle.toggle.isOn)
            {
                MakeTogglesMatchLatencyState();
                return;
            }
            int voiceRangeIndex = voiceRangeManager.GetVoiceRangeDefinition(toggle.voiceRangeInternalName).index;
            VoiceRangePlayerData localPlayer = voiceRangeManager.LocalPlayer;
            if (localPlayer.latencyVoiceRangeIndex != voiceRangeIndex)
                voiceRangeManager.SendSetVoiceRangeIndexIA(voiceRangeIndex, localPlayer);
        }

        [VoiceRangeEvent(VoiceRangeEventType.OnLocalVoiceRangeIndexChangedInLatency)]
        public void OnLocalVoiceRangeIndexChangedInLatency() => MakeTogglesMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeTogglesMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeTogglesMatchLatencyState();

        // TODO: This might not be needed, depends on how permission importing ends up affecting this.
        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp() => MakeTogglesMatchLatencyState();
    }
}
