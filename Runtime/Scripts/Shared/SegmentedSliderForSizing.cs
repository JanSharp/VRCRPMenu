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

        public float GetSize()
        {
            float count = slider.SegmentsCount;
            if ((count % 2f) == 0f)
            {
                Debug.LogError("[RPMenu] SegmentedSliderForSizing must use an odd number of segments", this);
                return 1f;
            }
            return Mathf.Pow(1f + percentagePerSegment, slider.Value - ((count - 1f) / 2f));
        }
    }
}
