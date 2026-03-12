using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RPMenuRevertTPButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public Button button;
        public TextMeshProUGUI label;
        public Selectable labelSelectable;
        public bool singleLine;
        protected string baseLabelText;

        protected VRCPlayerApi localPlayer;

        protected Vector3 positionForTooltip;
        protected float timeAtPositionBeforeLastTP;
        protected uint elapsedSeconds;
        protected string formattedElapsedSeconds;

        private bool interactable;
        private bool isHovered;

        private bool isShowingTooltip;

        public void OnClick()
        {
            teleportManager.RevertTeleport();
        }

        private void OnDisable()
        {
            OnPointerExit();
        }

        public void OnPointerEnter()
        {
            isHovered = true;
            UpdateTooltipShownState();
        }

        public void OnPointerExit()
        {
            isHovered = false;
            UpdateTooltipShownState();
        }

        [RPMenuTeleportEvent(RPMenuTeleportEventType.OnRPMenuRevertTPStateChanged)]
        public void OnRPMenuRevertTPStateChanged()
        {
            positionForTooltip = teleportManager.PositionBeforeLastTP;
            timeAtPositionBeforeLastTP = teleportManager.TimeAtPositionBeforeLastTP;
            elapsedSeconds = uint.MaxValue;
            interactable = teleportManager.HasPositionBeforeLastTP;
            button.interactable = interactable;
            labelSelectable.interactable = interactable;
            UpdateTooltipShownState();
        }

        private void UpdateTooltipShownState()
        {
            if (!interactable || !isHovered)
            {
                if (!isShowingTooltip)
                    return;
                isShowingTooltip = false;
                label.text = baseLabelText;
                updateManager.Deregister(this);
                return;
            }
            if (isShowingTooltip)
                return;
            if (baseLabelText == null)
            {
                baseLabelText = label.text;
                localPlayer = Networking.LocalPlayer;
            }
            isShowingTooltip = true;
            updateManager.Register(this);
            CustomUpdate();
        }

        public void CustomUpdate()
        {
            UpdateTimeAndDistanceTooltip();
        }

        protected void UpdateTimeAndDistanceTooltip()
        {
            uint seconds = (uint)(Time.time - timeAtPositionBeforeLastTP);
            if (seconds != elapsedSeconds)
            {
                elapsedSeconds = seconds;
                formattedElapsedSeconds = FormatTime(seconds);
            }
            float distance = Vector3.Distance(localPlayer.GetPosition(), positionForTooltip);
            label.text = $"{baseLabelText}{(singleLine ? " - " : "\n")}{formattedElapsedSeconds} ago, {distance:0.0} m";
        }

        protected string FormatTime(uint seconds)
        {
            uint minutes = seconds / 60u;
            seconds -= minutes * 60u;
            if (minutes < 60u)
                return $"{minutes}:{seconds:d2}";
            uint hours = minutes / 60u;
            minutes -= hours * 60u;
            return $"{hours}:{minutes:d2}:{seconds:d2}";
        }
    }
}
