using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsFacingOptions : UdonSharpBehaviour
    {
        [SerializeField] private Slider facingSlider;
        [SerializeField] private Selectable facingSliderHandle; // Just used for visualization.
        [SerializeField] private Toggle randomizeToggle;

        private float facingSliderMin;
        private float facingSliderMax;

        private float yRotation;
        private Quaternion offsetRotation;
        public Quaternion OffsetRotation => offsetRotation;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            facingSliderMin = facingSlider.minValue;
            facingSliderMax = facingSlider.maxValue;
            ReadYRotationFromSlider();
            CalculateOffsetRotation();
        }

        private float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            value = (value - oldMin) / (oldMax - oldMin); // Convert to 0-1;
            return value * (newMax - newMin) + newMin;
        }

        public void OnFacingSliderValueChanged()
        {
            randomizeToggle.SetIsOnWithoutNotify(false);
            facingSliderHandle.interactable = true;
            if (!facingSlider.wholeNumbers)
            {
                facingSlider.SetValueWithoutNotify(Mathf.Round(facingSlider.value));
                facingSlider.wholeNumbers = true;
            }
            ReadYRotationFromSlider();
            CalculateOffsetRotation();
        }

        private void ReadYRotationFromSlider()
        {
            yRotation = Remap(facingSlider.value, facingSliderMin, facingSliderMax, -180f, 180f);
        }

        public void OnRandomizeValueChanged()
        {
            randomizeToggle.SetIsOnWithoutNotify(true);
            facingSliderHandle.interactable = false;
            facingSlider.wholeNumbers = false;
            yRotation = Random.Range(-180f, 180f);
            facingSlider.SetValueWithoutNotify(Remap(yRotation, -180f, 180f, facingSliderMin, facingSliderMax));
            CalculateOffsetRotation();
        }

        private void CalculateOffsetRotation()
        {
            offsetRotation = Quaternion.AngleAxis(yRotation, Vector3.up);
        }
    }
}
