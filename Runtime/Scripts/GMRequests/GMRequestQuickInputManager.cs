using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(GMRequestsQuickInputEventAttribute), typeof(GMRequestsQuickInputEventType))]
    public class GMRequestQuickInputManager : GMRequestQuickInputManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        private bool isInProgress = false;
        public override bool IsInProgress => isInProgress;
        private float progress = 0f;
        public override float Progress => progress;
        private const float TotalHoldDuration = 0.7f;

        private float lastInputUseEventTime;
        private bool isHoldingRightTrigger;
        private bool isHoldingRightStickDown;
        private const float DownThreshold = -0.5f;
        private bool isHoldingBoth;

        private uint ignoreInputCounter;
        private bool inputIsIgnored;

        private bool isInVR;

        private void Start()
        {
            isInVR = Networking.LocalPlayer.IsUserInVR();
        }

#if RP_MENU_DEBUG
        private void Update()
        {
            UpdateIsHoldingBoth();
        }
#endif

        public override void IncrementIgnoreInput()
        {
            ignoreInputCounter++;
            inputIsIgnored = true;
            if (isInProgress)
                Abort();
        }

        public override void DecrementIgnoreInput()
        {
            if (ignoreInputCounter == 0u)
            {
                Debug.LogError($"[RPMenu] Attempt to {nameof(DecrementIgnoreInput)} more often than "
                    + $"{nameof(IncrementIgnoreInput)} on the {nameof(GMRequestQuickInputManager)} script.");
                return;
            }
            inputIsIgnored = (--ignoreInputCounter) != 0u;
            PotentiallyBegin();
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            // No isInVR check here to save that tiny bit of performance.
            // It won't activate in desktop anyway because InputUse checks isInVR.
            isHoldingRightStickDown = value <= DownThreshold;
            if (isHoldingRightTrigger) // Also a micro optimization.
                UpdateIsHoldingBoth();
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!isInVR)
                return;
            // NOTE: Comment and input deduplication copied from CustomInteractHandManager.cs
            // Ignore multiple InputUse events in the same frame... because for some unexplainable reason
            // VRChat is raising the InputUse event twice when I click the mouse button once.
            float timeTime = Time.time;
            if (args.handType != HandType.RIGHT || lastInputUseEventTime == timeTime)
                return;
            lastInputUseEventTime = timeTime;
            isHoldingRightTrigger = value;
            UpdateIsHoldingBoth();
        }

        private void UpdateIsHoldingBoth()
        {
#if RP_MENU_DEBUG
            bool newValue = Input.GetKey(KeyCode.R) || isHoldingRightTrigger && isHoldingRightStickDown;
#else
            bool newValue = isHoldingRightTrigger && isHoldingRightStickDown;
#endif
            if (isHoldingBoth == newValue)
                return;
            isHoldingBoth = newValue;
            PotentiallyBegin();
        }

        private void PotentiallyBegin()
        {
            if (!isHoldingBoth || isInProgress || inputIsIgnored)
                return;
            isInProgress = true;
            RaiseOnGMRequestQuickInputBegin();
            updateManager.Register(this);
        }

        private void Complete()
        {
            isInProgress = false;
            progress = 1f;
            RaiseOnGMRequestQuickInputCompleted();
            progress = 0f;
            updateManager.Deregister(this);
        }

        private void Abort()
        {
            isInProgress = false;
            progress = 0f;
            RaiseOnGMRequestQuickInputAborted();
            updateManager.Deregister(this);
        }

        public void CustomUpdate()
        {
            if (isHoldingBoth)
            {
                progress += Time.deltaTime / TotalHoldDuration;
                if (progress >= 1f)
                {
                    Complete();
                    return;
                }
            }
            else
            {
                progress -= Time.deltaTime / TotalHoldDuration;
                if (progress <= 0f)
                {
                    Abort();
                    return;
                }
            }
            RaiseOnGMRequestQuickInputUpdate();
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestQuickInputBeginListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestQuickInputUpdateListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestQuickInputCompletedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestQuickInputAbortedListeners;

        private void RaiseOnGMRequestQuickInputBegin()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestQuickInputBeginListeners, nameof(GMRequestsQuickInputEventType.OnGMRequestQuickInputBegin));
        }

        private void RaiseOnGMRequestQuickInputUpdate()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestQuickInputUpdateListeners, nameof(GMRequestsQuickInputEventType.OnGMRequestQuickInputUpdate));
        }

        private void RaiseOnGMRequestQuickInputCompleted()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestQuickInputCompletedListeners, nameof(GMRequestsQuickInputEventType.OnGMRequestQuickInputCompleted));
        }

        private void RaiseOnGMRequestQuickInputAborted()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestQuickInputAbortedListeners, nameof(GMRequestsQuickInputEventType.OnGMRequestQuickInputAborted));
        }

        #endregion
    }
}
