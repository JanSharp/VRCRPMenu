using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsFlyingType : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;

        [SerializeField] private NoClipSettingsFlyingTypeToggle[] toggles;
        public Selectable[] selectablesToDisable;
        public SegmentedSlider[] slidersToDisable;

        private void MakeSettingsMatchLatencyState()
        {
            bool enabled = noClipSettingsManager.LatencyNoClipEnabled;
            NoClipFlyingType flyingType = noClipSettingsManager.LatencyNoClipFlyingType;

            foreach (NoClipSettingsFlyingTypeToggle toggle in toggles)
                toggle.toggle.SetIsOnWithoutNotify(enabled
                    ? (!toggle.isNone && toggle.flyingType == flyingType)
                    : toggle.isNone);

            foreach (Selectable selectable in selectablesToDisable)
                selectable.interactable = enabled;
            foreach (SegmentedSlider slider in slidersToDisable)
                slider.Interactable = enabled;
        }

        public void OnValueChanged(NoClipSettingsFlyingTypeToggle toggle)
        {
            if (!toggle.toggle.isOn)
            {
                toggle.toggle.SetIsOnWithoutNotify(true);
                return;
            }
            if (toggle.isNone)
            {
                noClipSettingsManager.SendSetNoClipEnabledIA(noClipSettingsManager.LocalNoClipSettingsPlayerData, false);
                return;
            }
            noClipSettingsManager.SendSetNoClipFlyingTypeIA(noClipSettingsManager.LocalNoClipSettingsPlayerData, toggle.flyingType);
            // Latency state is not reliable enough to avoid sending the enabled IA even if it is already enabled.
            noClipSettingsManager.SendSetNoClipEnabledIA(noClipSettingsManager.LocalNoClipSettingsPlayerData, true);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeSettingsMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeSettingsMatchLatencyState();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipEnabledChanged)]
        public void OnLocalLatencyNoClipEnabledChanged() => MakeSettingsMatchLatencyState();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipFlyingTypeChanged)]
        public void OnLocalLatencyNoClipFlyingTypeChanged() => MakeSettingsMatchLatencyState();
    }
}
