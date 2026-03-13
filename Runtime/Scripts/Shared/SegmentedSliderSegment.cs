using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SegmentedSliderSegment : UdonSharpBehaviour
    {
        [System.NonSerialized] public uint index;
        public SegmentedSlider slider;
        public Button button;
        public Image fillImage;
        [Tooltip("Only used when Change Each Segment Size is true.")]
        public RectTransform resizableContainer;

        public void OnClick() => slider.OnClick(this);

        public void OnPointerEnter() => slider.OnPointerEnter(this);

        public void OnPointerExit() => slider.OnPointerExit(this);
    }
}
