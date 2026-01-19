using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuUISoundsSettings : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuSettingsManagerAPI menuSettingsManager;

        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Selectable mutedSelectable;
        [SerializeField] private Selectable unmutedSelectable;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Selectable volumeFillSelectable;

        private void MatchLatencyState()
        {
            MakeEnabledMatchLatency();
            MakeVolumeMatchLatency();
        }

        private void MakeEnabledMatchLatency()
        {
            bool enabled = menuSettingsManager.LatencyUISoundsEnabled;
            enabledToggle.SetIsOnWithoutNotify(enabled);
            mutedSelectable.interactable = !enabled;
            unmutedSelectable.interactable = enabled;
            volumeFillSelectable.interactable = enabled;
        }

        private void MakeVolumeMatchLatency()
        {
            float volume = menuSettingsManager.LatencyUISoundsVolume;
            float min = volumeSlider.minValue;
            float range = volumeSlider.maxValue - min;
            volume = min + volume * range;
            if (volumeSlider.wholeNumbers)
                volume = Mathf.Round(volume);
            volumeSlider.SetValueWithoutNotify(volume);
        }

        public void OnEnabledToggleValueChanged()
        {
            menuSettingsManager.SendSetUISoundsEnabledIA(menuSettingsManager.LocalPlayerSettings, enabledToggle.isOn);
        }

        public void OnVolumeSliderValueChanged()
        {
            float volume = volumeSlider.value;
            float min = volumeSlider.minValue;
            float range = volumeSlider.maxValue - min;
            volume = (volume - min) / range;
            menuSettingsManager.SendSetUISoundsVolumeIARateLimited(volume);
            if (!menuSettingsManager.LatencyUISoundsEnabled)
                menuSettingsManager.SendSetUISoundsEnabledIA(menuSettingsManager.LocalPlayerSettings, true);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MatchLatencyState();

        [MenuSettingsEvent(MenuSettingsEventType.OnLocalLatencyUISoundsEnabledSettingChanged)]
        public void OnLocalLatencyUISoundsEnabledSettingChanged() => MakeEnabledMatchLatency();

        [MenuSettingsEvent(MenuSettingsEventType.OnLocalLatencyUISoundsVolumeSettingChanged)]
        public void OnLocalLatencyUISoundsVolumeSettingChanged() => MakeVolumeMatchLatency();
    }
}
