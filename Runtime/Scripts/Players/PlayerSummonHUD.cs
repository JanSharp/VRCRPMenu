using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSummonHUD : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSummonManagerAPI summonManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        [HideInInspector][SerializeField][SingletonReference] private HUDManagerAPI hudManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public Transform hudRoot;
        public Image progressImage;
        public string hudOrder = "em[players]-m[summon]";

        private bool isShown = false;
        private float passedTime;
        private float currentSummonDelay;

        private void Start()
        {
            hudManager.AddHUDElement(hudRoot, hudOrder, isShown: false);
        }

        public void CustomUpdate()
        {
            passedTime += Time.deltaTime;
            progressImage.fillAmount = Mathf.Clamp01(1f - passedTime / currentSummonDelay);
        }

        private void ShowHideHUD(bool show)
        {
            if (show == isShown)
                return;
            isShown = show;
            if (isShown)
            {
                hudManager.ShowHUDElement(hudRoot);
                updateManager.Register(this);
            }
            else
            {
                hudManager.HideHUDElement(hudRoot);
                updateManager.Deregister(this);
                progressImage.fillAmount = 1f;
            }
        }

        [PlayerSummonEvent(PlayerSummonEventType.OnLocalPlayerSummonEnqueued)]
        public void OnLocalPlayerSummonEnqueued()
        {
            passedTime = 0f;
            currentSummonDelay = summonManager.SummonTargetTime - summonManager.SummonEnqueueTime;
            ShowHideHUD(true);
        }

        [PlayerSummonEvent(PlayerSummonEventType.OnLocalPlayerSummoned)]
        public void OnLocalPlayerSummoned()
        {
            ShowHideHUD(false);
        }
    }
}
