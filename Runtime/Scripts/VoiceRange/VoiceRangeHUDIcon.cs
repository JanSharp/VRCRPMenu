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
        Blinking,
        Static,
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

        private const float FadeDuration = 0.4f;
        private float blinksPerSecondWithTAU;
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
            iconState = VoiceRangeHUDIconState.Static;
            updateManager.Deregister(this);
        }

        public void SetBlinksPerSecond(float blinksPerSecondWithTAU)
        {
            this.blinksPerSecondWithTAU = blinksPerSecondWithTAU;
            if (blinksPerSecondWithTAU != 0f)
            {
                if (iconState == VoiceRangeHUDIconState.Static)
                {
                    iconState = VoiceRangeHUDIconState.Blinking;
                    updateManager.Register(this);
                }
            }
            else if (iconState == VoiceRangeHUDIconState.Blinking)
                SetIconStateToStatic();
        }

        public void FadeIn()
        {
            if (iconState != VoiceRangeHUDIconState.Off && iconState != VoiceRangeHUDIconState.FadingOut)
                return;
            fadeElapsedTime = 0;
            fadeSourceColor = iconImage.color;
            fadeDestinationColor = GetColorAtCurrentBlinkingElapsedTime();
            iconState = VoiceRangeHUDIconState.FadingIn;
            updateManager.Register(this);
            Show();
        }

        private void FinishedFadingIn()
        {
            if (blinksPerSecondWithTAU == 0f)
                SetIconStateToStatic(); // fadeDestinationColor may not match maxColor.
            else
                iconState = VoiceRangeHUDIconState.Blinking;
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
            float t = (Mathf.Cos(blinkingElapsedTime * blinksPerSecondWithTAU) + 1f) / 2f;
            // t is 1 when blinkingElapsedTime is 0.
            return Color.Lerp(minColor, maxColor, t);
        }

        public void CustomUpdate()
        {
            if (iconState == VoiceRangeHUDIconState.Blinking)
            {
                blinkingElapsedTime += Time.deltaTime;
                // This is GetColorAtCurrentBlinkingElapsedTime inlined (aka copy pasted).
                float t = (Mathf.Cos(blinkingElapsedTime * blinksPerSecondWithTAU) + 1f) / 2f;
                iconImage.color = Color.Lerp(minColor, maxColor, t);
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
