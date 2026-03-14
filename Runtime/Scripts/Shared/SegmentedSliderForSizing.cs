using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SegmentedSliderForSizing : UdonSharpBehaviour
    {
        public SegmentedSlider slider;
        [Min(0f)]
        public float percentagePerSegment = 0.2f;
        private float middlePointIndex = 0u;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            middlePointIndex = slider.Value;
        }

        public float GetSize()
        {
            return Mathf.Pow(1f + percentagePerSegment, slider.Value - middlePointIndex);
        }
    }
}
