using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    public enum VoiceRangeHUDIconState
    {
        Off,
        FadingIn,
        FadingOut,
        MatchingVisualType,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeHUDIcon : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        public GameObject iconGo;
        public Image iconImage;
        public string voiceRangeInternalName;
        [System.NonSerialized] public VoiceRangeHUD hudController;
        [System.NonSerialized] public VoiceRangeDefinition resolvedDef;
        [System.NonSerialized] public Color offColor;
        [System.NonSerialized] public Color minColor;
        [System.NonSerialized] public Color maxColor;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        private const float TAU = Mathf.PI * 2f;
        public const float PulsesPerSecondWithTAU = 0.25f * TAU;
        public const float BlinkInterval = 4f;
        private VoiceRangeVisualizationType visualType = VoiceRangeVisualizationType.Static;

        public const float FadeDuration = 0.6f;
        private float fadeElapsedTime = 0f;
        private float blinkingElapsedTime = 0f;
        private VoiceRangeHUDIconState iconState = VoiceRangeHUDIconState.Off;
        private Color fadeSourceColor;
        private Color fadeDestinationColor;

        private bool isShown = false;

        private void Show()
        {
            if (isShown)
                return;
            isShown = true;
            hudController.IncrementShown();
            iconGo.SetActive(true);
        }

        private void Hide()
        {
            if (!isShown)
                return;
            isShown = false;
            hudController.DecrementShown();
            iconGo.SetActive(false);
        }

        private void SetIconStateToStatic()
        {
            iconImage.color = maxColor;
            visualType = VoiceRangeVisualizationType.Static;
            iconState = VoiceRangeHUDIconState.MatchingVisualType;
            updateManager.Deregister(this);
        }

        public void SetVisualType(VoiceRangeVisualizationType visualType)
        {
            if (this.visualType == visualType)
                return;
            if (iconState != VoiceRangeHUDIconState.MatchingVisualType)
            {
                this.visualType = visualType;
                return;
            }

            if (visualType == VoiceRangeVisualizationType.Static)
                SetIconStateToStatic();
            else
            {
                this.visualType = visualType;
                updateManager.Register(this);
            }
        }

        public void FadeIn()
        {
            if (iconState != VoiceRangeHUDIconState.Off && iconState != VoiceRangeHUDIconState.FadingOut)
                return;
            fadeElapsedTime = 0;
            if (visualType != VoiceRangeVisualizationType.Pulse)
                blinkingElapsedTime = 0f;
            fadeSourceColor = iconImage.color;
            fadeDestinationColor = GetColorAtCurrentBlinkingElapsedTime();
            iconState = VoiceRangeHUDIconState.FadingIn;
            updateManager.Register(this);
            Show();
        }

        private void FinishedFadingIn()
        {
            if (visualType == VoiceRangeVisualizationType.Static)
                SetIconStateToStatic(); // fadeDestinationColor may not match maxColor.
            else
                iconState = VoiceRangeHUDIconState.MatchingVisualType;
        }

        public void FadeOut()
        {
            if (iconState == VoiceRangeHUDIconState.Off || iconState == VoiceRangeHUDIconState.FadingOut)
                return;
            fadeElapsedTime = 0;
            fadeSourceColor = iconImage.color;
            fadeDestinationColor = offColor;
            iconState = VoiceRangeHUDIconState.FadingOut;
            updateManager.Register(this);
        }

        private void FinishedFadingOut()
        {
            blinkingElapsedTime = 0f;
            iconState = VoiceRangeHUDIconState.Off;
            updateManager.Deregister(this);
            Hide();
        }

        private Color GetColorAtCurrentBlinkingElapsedTime()
        {
            if (visualType != VoiceRangeVisualizationType.Pulse)
                return maxColor;
            float t = (Mathf.Cos(blinkingElapsedTime * PulsesPerSecondWithTAU) + 1f) / 2f;
            // t is 1 when blinkingElapsedTime is 0.
            return Color.Lerp(minColor, maxColor, t);
        }

        public void CustomUpdate()
        {
            if (iconState == VoiceRangeHUDIconState.MatchingVisualType)
            {
                blinkingElapsedTime += Time.deltaTime;
                if (visualType == VoiceRangeVisualizationType.Pulse)
                {
                    // This is GetColorAtCurrentBlinkingElapsedTime inlined (aka copy pasted).
                    float t = (Mathf.Cos(blinkingElapsedTime * PulsesPerSecondWithTAU) + 1f) / 2f;
                    iconImage.color = Color.Lerp(minColor, maxColor, t);
                }
                else // Blink
                {
                    float t = (blinkingElapsedTime % BlinkInterval) / BlinkInterval;
                    iconImage.color = t < 0.5f ? maxColor : minColor;
                }
                return;
            }

            // Fading in or fading out.
            fadeElapsedTime += Time.deltaTime;
            if (fadeElapsedTime < FadeDuration)
                iconImage.color = Color.Lerp(fadeSourceColor, fadeDestinationColor, fadeElapsedTime / FadeDuration);
            else
            {
                iconImage.color = fadeDestinationColor;
                if (iconState == VoiceRangeHUDIconState.FadingIn)
                    FinishedFadingIn();
                else
                    FinishedFadingOut();
            }
        }
    }
}
