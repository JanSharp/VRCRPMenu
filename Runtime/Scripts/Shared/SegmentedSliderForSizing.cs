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
        /// <summary>
        /// <para>The index at which th size will be 1.</para>
        /// </summary>
        private uint middlePointIndex = 0u;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            middlePointIndex = slider.Value;
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuActivePageChanged)]
        public void OnMenuActivePageChanged()
        {
            // No real need to check if the page changed to or from the page this slider is on.
            slider.Value = middlePointIndex;
        }

        public float GetSize()
        {
            return Mathf.Pow(1f + percentagePerSegment, slider.Value - (float)middlePointIndex);
        }
    }
}
