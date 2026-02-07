using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestNoClipMovementModes : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipMovementAPI noClipMovement;

        public ToggleGroup stillToggleGroup;
        public Toggle stillVelocityToggle;
        public Toggle stillFakeGroundToggle;

        public ToggleGroup movingToggleGroup;
        public Toggle movingVelocityToggle;
        public Toggle movingComboToggle;
        public Toggle movingTeleportToggle;

        public ToggleGroup verticalMovementToggleGroup;
        public Toggle verticalMovementNoneToggle;
        public Toggle verticalMovementHeadLocalSpaceToggle;
        public Toggle verticalMovementWorldSpaceToggle;

        public Toggle inputSmoothingToggle;
        public Toggle linkedInputSmoothingToggle;
        public Slider inputSmoothingSlider;
        public Selectable inputSmoothingFillSelectable;

        public Toggle setVelocityToZeroWhileTeleportingToggle;
        public Toggle linkedSetVelocityToZeroWhileTeleportingToggle;

        private void Start()
        {
            stillToggleGroup.allowSwitchOff = true;
            stillVelocityToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileStill == NoClipModeWhileStill.Velocity);
            stillFakeGroundToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileStill == NoClipModeWhileStill.FakeGround);
            stillToggleGroup.allowSwitchOff = false;

            movingToggleGroup.allowSwitchOff = true;
            movingVelocityToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileMoving == NoClipModeWhileMoving.Velocity);
            movingComboToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileMoving == NoClipModeWhileMoving.Combo);
            movingTeleportToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileMoving == NoClipModeWhileMoving.Teleport);
            movingToggleGroup.allowSwitchOff = false;

            verticalMovementToggleGroup.allowSwitchOff = true;
            verticalMovementNoneToggle.SetIsOnWithoutNotify(noClipMovement.VerticalMovement == NoClipVerticalMovementType.None);
            verticalMovementHeadLocalSpaceToggle.SetIsOnWithoutNotify(noClipMovement.VerticalMovement == NoClipVerticalMovementType.HeadLocalSpace);
            verticalMovementWorldSpaceToggle.SetIsOnWithoutNotify(noClipMovement.VerticalMovement == NoClipVerticalMovementType.WorldSpace);
            verticalMovementToggleGroup.allowSwitchOff = false;

            inputSmoothingToggle.SetIsOnWithoutNotify(true);
            linkedInputSmoothingToggle.SetIsOnWithoutNotify(true);
            inputSmoothingFillSelectable.interactable = true;
            float value = noClipMovement.InputSmoothingDuration;
            float min = inputSmoothingSlider.minValue;
            float range = inputSmoothingSlider.maxValue - min;
            value = min + value * range;
            if (inputSmoothingSlider.wholeNumbers)
                value = Mathf.Round(value);
            inputSmoothingSlider.SetValueWithoutNotify(value);

            setVelocityToZeroWhileTeleportingToggle.SetIsOnWithoutNotify(noClipMovement.SetVelocityToZeroWhileTeleporting);
            linkedSetVelocityToZeroWhileTeleportingToggle.SetIsOnWithoutNotify(noClipMovement.SetVelocityToZeroWhileTeleporting);
        }

        public void OnStillToggleValueChanged()
        {
            if (stillVelocityToggle.isOn)
                noClipMovement.ModeWhileStill = NoClipModeWhileStill.Velocity;
            else if (stillFakeGroundToggle.isOn)
                noClipMovement.ModeWhileStill = NoClipModeWhileStill.FakeGround;
        }

        public void OnMovingToggleValueChanged()
        {
            if (movingVelocityToggle.isOn)
                noClipMovement.ModeWhileMoving = NoClipModeWhileMoving.Velocity;
            else if (movingComboToggle.isOn)
                noClipMovement.ModeWhileMoving = NoClipModeWhileMoving.Combo;
            else if (movingTeleportToggle.isOn)
                noClipMovement.ModeWhileMoving = NoClipModeWhileMoving.Teleport;
        }

        public void OnVerticalMovementToggleValueChanged()
        {
            if (verticalMovementNoneToggle.isOn)
                noClipMovement.VerticalMovement = NoClipVerticalMovementType.None;
            else if (verticalMovementHeadLocalSpaceToggle.isOn)
                noClipMovement.VerticalMovement = NoClipVerticalMovementType.HeadLocalSpace;
            else if (verticalMovementWorldSpaceToggle.isOn)
                noClipMovement.VerticalMovement = NoClipVerticalMovementType.WorldSpace;
        }

        public void OnInputSmoothingToggleValueChanged()
        {
            linkedInputSmoothingToggle.SetIsOnWithoutNotify(inputSmoothingToggle.isOn);
            inputSmoothingFillSelectable.interactable = inputSmoothingToggle.isOn;
            if (inputSmoothingToggle.isOn)
                ApplyInputSmoothingDurationFromSlider();
            else
                noClipMovement.InputSmoothingDuration = 0f;
        }

        public void OnInputSmoothingSliderValueChanged()
        {
            inputSmoothingToggle.SetIsOnWithoutNotify(true);
            linkedInputSmoothingToggle.SetIsOnWithoutNotify(true);
            inputSmoothingFillSelectable.interactable = true;
            ApplyInputSmoothingDurationFromSlider();
        }

        private void ApplyInputSmoothingDurationFromSlider()
        {
            float value = inputSmoothingSlider.value;
            float min = inputSmoothingSlider.minValue;
            float range = inputSmoothingSlider.maxValue - min;
            value = (value - min) / range;
            noClipMovement.InputSmoothingDuration = value;
        }

        public void OnSetVelocityToZeroWhileTeleportingToggleValueChanged()
        {
            noClipMovement.SetVelocityToZeroWhileTeleporting = setVelocityToZeroWhileTeleportingToggle.isOn;
            linkedSetVelocityToZeroWhileTeleportingToggle.SetIsOnWithoutNotify(setVelocityToZeroWhileTeleportingToggle.isOn);
        }
    }
}
