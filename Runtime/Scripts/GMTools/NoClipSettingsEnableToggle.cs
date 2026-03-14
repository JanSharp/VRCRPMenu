using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsEnableToggle : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;

        public Toggle toggle;
        public Toggle linkedToggle;
        public Selectable[] selectablesToDisable;
        public NoClipSettingsFlyingTypeToggle[] flyingTypeTogglesToDisable;
        public SegmentedSlider[] slidersToDisable;

        public void OnValueChanged()
        {
            bool isOn = toggle.isOn;
            linkedToggle.SetIsOnWithoutNotify(isOn); // Technically redundant, but keeping it for clarity.
            noClipSettingsManager.SendSetNoClipEnabledIA(noClipSettingsManager.LocalNoClipSettingsPlayerData, isOn);
        }

        private void MakeSettingsMatchLatencyState()
        {
            bool enabled = noClipSettingsManager.LatencyNoClipEnabled;
            toggle.SetIsOnWithoutNotify(enabled);
            linkedToggle.SetIsOnWithoutNotify(enabled);
            foreach (Selectable selectable in selectablesToDisable)
                selectable.interactable = enabled;
            foreach (NoClipSettingsFlyingTypeToggle toggle in flyingTypeTogglesToDisable)
                toggle.Interactable = enabled;
            foreach (SegmentedSlider slider in slidersToDisable)
                slider.Interactable = enabled;
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeSettingsMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeSettingsMatchLatencyState();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipEnabledChanged)]
        public void OnLocalLatencyNoClipEnabledChanged() => MakeSettingsMatchLatencyState();
    }
}
