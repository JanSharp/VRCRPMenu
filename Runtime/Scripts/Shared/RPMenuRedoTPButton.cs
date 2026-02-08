using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RPMenuRedoTPButton : RPMenuUndoRedoTPButton
    {
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public Button button;
        public Selectable labelSelectable;

        private bool interactable;
        private bool isHovered;
        private CorePlayerData redoAblePlayer;

        private bool tooltipIsShown;
        private bool redoAbleLocationIsPlayer;
        private RPPlayerData playerInTooltip;

        public void OnClick()
        {
            teleportManager.RedoTeleport();
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

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged)]
        public void OnRPPlayerDataOverriddenDisplayNameChanged()
        {
            if (playersBackendManager.RPPlayerDataForEvent == playerInTooltip)
                UpdateTooltip();
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged)]
        public void OnRPPlayerDataCharacterNameChanged()
        {
            if (playersBackendManager.RPPlayerDataForEvent == playerInTooltip)
                UpdateTooltip();
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp() // This would/is only needed if the button is outside of the menu entirely.
        {
            if (tooltipIsShown)
                UpdateTooltip();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            if (playerDataManager.PlayerDataForEvent == redoAblePlayer)
                UpdateInteractableState();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline()
        {
            if (playerDataManager.PlayerDataForEvent == redoAblePlayer)
                UpdateInteractableState();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline()
        {
            if (playerDataManager.PlayerDataForEvent == redoAblePlayer)
                UpdateInteractableState();
        }

        [RPMenuTeleportEvent(RPMenuTeleportEventType.OnRPMenuTeleportUndoRedoStateChanged)]
        public void OnRPMenuTeleportUndoRedoStateChanged()
        {
            positionForTooltip = teleportManager.RedoAblePosition;
            undoAbleActionTakenAtTime = teleportManager.UndoAbleActionTakenAtTime;
            elapsedSeconds = uint.MaxValue;
            redoAbleLocationIsPlayer = teleportManager.RedoAbleLocationIsPlayer;
            UpdateInteractableState();
        }

        private void UpdateInteractableState()
        {
            if (!teleportManager.HasUndoData || teleportManager.IsAtUndoAbleLocation)
            {
                redoAblePlayer = null;
                SetInteractable(false);
                return;
            }
            // Has undo data and is at redo able location.

            if (redoAbleLocationIsPlayer)
            {
                redoAblePlayer = teleportManager.RedoAblePlayer;
                if (redoAblePlayer != null && redoAblePlayer.isDeleted)
                    redoAblePlayer = null;
                SetInteractable(redoAblePlayer != null);
            }
            else
            {
                redoAblePlayer = null;
                SetInteractable(true);
            }
        }

        private void SetInteractable(bool interactable)
        {
            this.interactable = interactable;
            button.interactable = interactable;
            labelSelectable.interactable = interactable;
            UpdateTooltipShownState();
        }

        private void UpdateTooltipShownState()
        {
            if (!interactable || !isHovered)
            {
                if (!tooltipIsShown)
                    return;
                tooltipIsShown = false;
                playerInTooltip = null;
                label.text = baseLabelText;
                updateManager.Deregister(this);
                return;
            }
            playerInTooltip = redoAblePlayer == null ? null : playersBackendManager.GetRPPlayerData(redoAblePlayer);
            if (tooltipIsShown)
            {
                UpdateTooltip();
                PotentiallyRegisterForCustomUpdate();
                return;
            }
            if (baseLabelText == null)
            {
                baseLabelText = label.text;
                localPlayer = Networking.LocalPlayer;
            }
            tooltipIsShown = true;
            UpdateTooltip();
            PotentiallyRegisterForCustomUpdate();
        }

        private void PotentiallyRegisterForCustomUpdate()
        {
            if (redoAbleLocationIsPlayer)
                updateManager.Deregister(this);
            else
                updateManager.Register(this);
        }

        public void CustomUpdate()
        {
            UpdateTimeAndDistanceTooltip();
        }

        private void UpdateTooltip()
        {
            if (redoAbleLocationIsPlayer)
                label.text = $"{baseLabelText}{(singleLine ? " - " : "\n")}{playerInTooltip.PlayerDisplayNameWithCharacterName}";
            else
                UpdateTimeAndDistanceTooltip();
        }
    }
}
