using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsSpeed : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;

        public SegmentedSlider speedSlider;
        public float[] speedValues;

        public void OnValueChanged()
        {
            float speed = speedValues[speedSlider.Value];
            noClipSettingsManager.SendSetNoClipSpeedIA(noClipSettingsManager.LocalNoClipSettingsPlayerData, speed);
        }

        private void MakeSettingsMatchLatencyState()
        {
            int valuesCount = speedValues.Length;
            float speed = noClipSettingsManager.LatencyNoClipSpeed;
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

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipSpeedChanged)]
        public void OnLocalLatencyNoClipSpeedChanged() => MakeSettingsMatchLatencyState();
    }
}
