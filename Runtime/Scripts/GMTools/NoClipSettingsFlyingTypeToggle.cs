using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsFlyingTypeToggle : UdonSharpBehaviour
    {
        [SerializeField] private NoClipSettingsFlyingType settings;
        [SerializeField] private Button button;
        [Space]
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
        [Space]
        public Graphic targetGraphic;
        public float transitionTime = 0.1f;
        [Space]
        public NoClipFlyingType flyingType;

        [SerializeField] private bool isOn = false;
        public bool IsOn => isOn;

        [SerializeField] private bool interactable = true;
        public bool Interactable
        {
            get => interactable;
            set
            {
                if (interactable == value)
                    return;
                interactable = value;
                button.interactable = interactable;
                UpdateTargetGraphic();
            }
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            button.interactable = interactable;
            UpdateTargetGraphic();
        }

        private void OnDisable()
        {
            UpdateTargetGraphic();
        }

        public void OnClick()
        {
            SetIsOnWithoutNotify(!isOn);
            settings.OnValueChanged(this);
        }

        public void SetIsOnWithoutNotify(bool isOn)
        {
            if (this.isOn == isOn)
                return;
            this.isOn = isOn;
            UpdateTargetGraphic();
        }

        private void UpdateTargetGraphic()
        {
            targetGraphic.CrossFadeColor(
                isOn
                    ? (interactable ? onColor : onDisabledColor)
                    : (interactable ? offColor : offDisabledColor),
                transitionTime,
                ignoreTimeScale: true,
                useAlpha: true);
        }
    }
}
