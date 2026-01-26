using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SegmentedSlider : UdonSharpBehaviour
    {
        [UIStyleColor(nameof(offColor))]
        public string offColorName;
        public Color offColor;
        [UIStyleColor(nameof(onColor))]
        public string onColorName;
        public Color onColor;
        [UIStyleColor(nameof(offDisabledColor))]
        public string offDisabledColorName;
        public Color offDisabledColor;
        [UIStyleColor(nameof(onDisabledColor))]
        public string onDisabledColorName;
        public Color onDisabledColor;
        [UIStyleColor(nameof(stayingOnColor))]
        public string stayingOnColorName;
        public Color stayingOnColor;
        [UIStyleColor(nameof(stayingOffColor))]
        public string stayingOffColorName;
        public Color stayingOffColor;
        [UIStyleColor(nameof(turningOnColor))]
        public string turningOnColorName;
        public Color turningOnColor;
        [UIStyleColor(nameof(turningOffColor))]
        public string turningOffColorName;
        public Color turningOffColor;

        private uint segmentsCount;
        [SerializeField] private SegmentedSliderSegment[] segments;
        [SerializeField] private float transitionTime = 0.1f;

        [SerializeField] private UdonBehaviour[] listeners;
        [SerializeField] private string[] listenerEventNames;

        [SerializeField] private bool interactable = true;
        public bool Interactable
        {
            get => interactable;
            set
            {
                if (interactable == value)
                    return;
                interactable = value;
                UpdateSegments();
            }
        }

        [Tooltip("Zero based")]
        [SerializeField] private uint value;
        public uint Value
        {
            get => value;
            set
            {
                if (this.value == value)
                    return;
                this.value = value;
                UpdateSegments();
                RaiseOnValueChanged();
            }
        }

        private bool isHovering;
        private uint hoveredValue;

        public void SetValueWithoutNotify(uint value)
        {
            this.value = value;
            UpdateSegments();
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            segmentsCount = (uint)segments.Length;
            for (uint i = 0; i < segmentsCount; i++)
            {
                SegmentedSliderSegment segment = segments[i];
                segment.index = i;
                segment.fillImage.CrossFadeColor(i <= value ? onColor : offColor, 0f, ignoreTimeScale: true, useAlpha: true);
            }
        }

        private void OnDisable()
        {
            isHovering = false;
            if (interactable)
                UpdateSegments();
        }

        public void OnClick(SegmentedSliderSegment segment)
        {
            if (!interactable)
                return;
            Value = segment.index;
        }

        public void OnPointerEnter(SegmentedSliderSegment segment)
        {
            isHovering = true;
            hoveredValue = segment.index;
            if (interactable)
                UpdateSegments();
        }

        public void OnPointerExit(SegmentedSliderSegment segment)
        {
            if (hoveredValue != segment.index)
                return;
            isHovering = false;
            if (interactable)
                UpdateSegments();
        }

        private void UpdateSegments()
        {
            if (!interactable)
            {
                for (uint i = 0; i < segmentsCount; i++)
                    segments[i].fillImage.CrossFadeColor(
                        i <= value ? onDisabledColor : offDisabledColor,
                        transitionTime,
                        ignoreTimeScale: true,
                        useAlpha: true);
                return;
            }
            for (uint i = 0; i < segmentsCount; i++)
                segments[i].fillImage.CrossFadeColor(
                    i <= value
                        ? (!isHovering ? onColor : i <= hoveredValue ? stayingOnColor : turningOffColor)
                        : (!isHovering ? offColor : i <= hoveredValue ? turningOnColor : stayingOffColor),
                    transitionTime,
                    ignoreTimeScale: true,
                    useAlpha: true);
        }

        private void RaiseOnValueChanged()
        {
            int length = listeners.Length;
            for (int i = 0; i < length; i++)
            {
                UdonBehaviour listener = listeners[i];
                if (listener != null)
                    listener.SendCustomEvent(listenerEventNames[i]);
            }
        }
    }
}
