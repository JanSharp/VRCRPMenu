using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RPMenuLastPlayerTPButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;

        public Button button;
        public TextMeshProUGUI label;
        public Selectable labelSelectable;
        public bool singleLine;
        protected string baseLabelText;

        private bool isHovered;
        private CorePlayerData lastPlayerTP;

        private bool tooltipIsShown;
        private RPPlayerData playerInTooltip;

        public void OnClick()
        {
            teleportManager.TeleportToLastPlayerTP();
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

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline()
        {
            if (playerDataManager.PlayerDataForEvent == lastPlayerTP)
                UpdateInteractableState();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline()
        {
            if (playerDataManager.PlayerDataForEvent == lastPlayerTP)
                UpdateInteractableState();
        }

        [RPMenuTeleportEvent(RPMenuTeleportEventType.OnRPMenuLastPlayerTPStateChanged)]
        public void OnRPMenuLastPlayerTPStateChanged()
        {
            UpdateInteractableState();
        }

        private void UpdateInteractableState()
        {
            if (!teleportManager.HasLastPlayerTP)
            {
                lastPlayerTP = null;
                SetInteractable(false);
                return;
            }

            lastPlayerTP = teleportManager.LastPlayerTP;
            SetInteractable(lastPlayerTP != null && !lastPlayerTP.isOffline);
        }

        private void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            labelSelectable.interactable = interactable;
            UpdateTooltipShownState();
        }

        private void UpdateTooltipShownState()
        {
            if (lastPlayerTP == null || !isHovered)
            {
                if (!tooltipIsShown)
                    return;
                tooltipIsShown = false;
                playerInTooltip = null;
                label.text = baseLabelText;
                return;
            }
            playerInTooltip = lastPlayerTP == null ? null : playersBackendManager.GetRPPlayerData(lastPlayerTP);
            if (tooltipIsShown)
            {
                UpdateTooltip();
                return;
            }
            if (baseLabelText == null)
                baseLabelText = label.text;
            tooltipIsShown = true;
            UpdateTooltip();
        }

        private void UpdateTooltip()
        {
            label.text = $"{baseLabelText}{(singleLine ? " - " : "\n")}{playerInTooltip.PlayerDisplayNameWithCharacterName}";
        }
    }
}
