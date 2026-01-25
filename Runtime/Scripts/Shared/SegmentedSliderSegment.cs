using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SegmentedSliderSegment : UdonSharpBehaviour
    {
        [System.NonSerialized] public uint index;
        public SegmentedSlider slider;
        public Image fillImage;

        public void OnClick() => slider.OnClick(this);

        public void OnPointerEnter() => slider.OnPointerEnter(this);

        public void OnPointerExit() => slider.OnPointerExit(this);
    }
}
