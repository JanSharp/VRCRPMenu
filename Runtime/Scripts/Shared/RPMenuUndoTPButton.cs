using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RPMenuUndoTPButton : RPMenuUndoRedoTPButton
    {
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public Button button;
        public Selectable labelSelectable;

        private bool interactable;
        private bool isHovered;

        private bool isShowingTooltip;

        public void OnClick()
        {
            teleportManager.UndoTeleport();
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

        [RPMenuTeleportEvent(RPMenuTeleportEventType.OnRPMenuTeleportUndoRedoStateChanged)]
        public void OnRPMenuTeleportUndoRedoStateChanged()
        {
            positionForTooltip = teleportManager.UndoAblePosition;
            undoAbleActionTakenAtTime = teleportManager.UndoAbleActionTakenAtTime;
            elapsedSeconds = uint.MaxValue;
            interactable = teleportManager.HasUndoData && teleportManager.IsAtUndoAbleLocation;
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
    }
}
