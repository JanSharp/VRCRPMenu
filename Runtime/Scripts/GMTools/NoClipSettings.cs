using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettings : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipManagerAPI noClipManager;

        public Toggle enabledToggle;
        public Toggle linkedEnabledToggle;
        [Tooltip("Null is allowed and supported")]
        public SegmentedSlider speedSlider;
        public float[] speedValues;

        public void OnEnableToggleValueChanged()
        {
            bool isOn = enabledToggle.isOn;
            linkedEnabledToggle.SetIsOnWithoutNotify(isOn); // Technically redundant, but keeping it for clarity.
            noClipManager.SendSetNoClipEnabledIA(noClipManager.LocalNoClipPlayerData, isOn);
        }

        public void OnSpeedSliderValueChanged()
        {
            float speed = speedValues[speedSlider.Value];
            noClipManager.SendSetNoClipSpeedIA(noClipManager.LocalNoClipPlayerData, speed);
        }

        private void MakeSettingsMatchLatencyState()
        {
            MakeEnabledToggleMatchLatencyState();
            MakeSpeedSliderMatchLatencyState();
        }

        private void MakeEnabledToggleMatchLatencyState()
        {
            enabledToggle.SetIsOnWithoutNotify(noClipManager.LatencyNoClipEnabled);
            linkedEnabledToggle.SetIsOnWithoutNotify(noClipManager.LatencyNoClipEnabled);
        }

        private void MakeSpeedSliderMatchLatencyState()
        {
            int valuesCount = speedValues.Length;
            if (speedSlider == null || valuesCount == 0)
                return;
            float speed = noClipManager.LatencyNoClipSpeed;
            float smallestDifference = float.PositiveInfinity;
            int closestSpeedIndex = -1;
            for (int i = 0; i < valuesCount; i++)
            {
                float difference = Mathf.Abs(speedValues[i] - speed);
                if (difference >= smallestDifference)
                    continue;
                smallestDifference = difference;
                closestSpeedIndex = i;
            }
            speedSlider.SetValueWithoutNotify((uint)closestSpeedIndex);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeSettingsMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeSettingsMatchLatencyState();

        [NoClipEvent(NoClipEventType.OnLocalLatencyNoClipEnabledChanged)]
        public void OnLocalLatencyNoClipEnabledChanged() => MakeEnabledToggleMatchLatencyState();

        [NoClipEvent(NoClipEventType.OnLocalLatencyNoClipSpeedChanged)]
        public void OnLocalLatencyNoClipSpeedChanged() => MakeSpeedSliderMatchLatencyState();
    }
}
