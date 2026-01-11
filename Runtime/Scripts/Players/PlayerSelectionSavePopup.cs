using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionSavePopup : DynamicDataSavePopup
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;
        protected override DynamicDataManager GetDynamicDataManager() => selectionManager;

        public RectTransform popupRoot;
        public Button saveAsGlobalButton;
        public Selectable saveAsGlobalLabel;
        public TMP_InputField groupNameField;
        public TextMeshProUGUI groupNameFieldPlaceholder;
        private string groupNameFieldPlaceholderNormalText;
        public Button undoOverwriteButton;
        public Selectable undoOverwriteLabelSelectable;
        public TextMeshProUGUI undoOverwriteLabel;
        private string undoOverwriteLabelNormalText;

        private bool isHoveringLocalSaveButton;

        public override void OnMenuManagerStart()
        {
            base.OnMenuManagerStart();
            groupNameFieldPlaceholderNormalText = groupNameFieldPlaceholder.text;
            undoOverwriteLabelNormalText = undoOverwriteLabel.text;
        }

        public void OnGroupNameValueChanged()
        {
            UpdateSaveAsGlobalInteractable();
        }

        private void UpdateSaveAsGlobalInteractable()
        {
            bool interactable = !string.IsNullOrWhiteSpace(groupNameField.text);
            saveAsGlobalButton.interactable = interactable;
            saveAsGlobalLabel.interactable = interactable;
        }

        public void OnSaveAsLocalClick()
        {
            menuManager.ClosePopup(popupRoot, doCallback: true);
            SendAddIA(isGlobal: false);
        }

        public void OnSaveAsGlobalClick()
        {
            menuManager.ClosePopup(popupRoot, doCallback: true);
            SendAddIA(isGlobal: true);
        }

        private void SendAddIA(bool isGlobal)
        {
            PlayerSelectionGroup group = selectionManager.SelectionGroupForSerialization;

            string dataName = groupNameField.text.Trim();
            groupNameField.SetTextWithoutNotify("");
            UpdateSaveAsGlobalInteractable();
            if (!isGlobal && dataName == "")
                dataName = GetFirstUnusedLocalGroupName();
            group.dataName = dataName;

            CorePlayerData[] players = new CorePlayerData[selectionManager.selectedPlayersCount];
            System.Array.Copy(selectionManager.selectedPlayers, players, players.Length);
            group.selectedPlayers = players;

            group.isGlobal = isGlobal;
            group.owningPlayer = playerDataManager.LocalPlayerData;
            selectionManager.SendAddIA(group);
        }

        public void OnSaveAsLocalPointerEnter()
        {
            isHoveringLocalSaveButton = true;
            PreviewGroupNameInPlaceholder();
        }

        public void OnSaveAsLocalPointerExit()
        {
            isHoveringLocalSaveButton = false;
            groupNameFieldPlaceholder.text = groupNameFieldPlaceholderNormalText;
        }

        public override void OnOverwriteButtonClick(DynamicDataOverwriteButton button)
        {
            menuManager.ClosePopup(popupRoot, doCallback: true);
            PlayerSelectionGroup toOverwrite = (PlayerSelectionGroup)button.dynamicData;
            PlayerSelectionGroup group = selectionManager.SelectionGroupForSerialization;

            CorePlayerData[] players = new CorePlayerData[selectionManager.selectedPlayersCount];
            System.Array.Copy(selectionManager.selectedPlayers, players, players.Length);
            group.selectedPlayers = players;

            group.dataName = toOverwrite.dataName;
            group.isGlobal = toOverwrite.isGlobal;
            group.owningPlayer = playerDataManager.LocalPlayerData;
            selectionManager.SendOverwriteIA(group, isUndo: false);
        }

        public override void OnUndoOverwriteClick()
        {
            PlayerSelectionGroup toRestore = (PlayerSelectionGroup)selectionManager.GetTopFromOverwriteUndoStack();
            if (toRestore == null)
                return;
            PlayerSelectionGroup group = selectionManager.SelectionGroupForSerialization;

            CorePlayerData[] selectedPlayers = toRestore.selectedPlayers;
            int nullCount = 0;
            foreach (CorePlayerData player in selectedPlayers)
                if (player == null || player.isDeleted)
                    nullCount++;
            if (nullCount == 0)
                group.selectedPlayers = selectedPlayers;
            else
            {
                CorePlayerData[] newPlayers = new CorePlayerData[selectedPlayers.Length - nullCount];
                int i = 0;
                foreach (CorePlayerData player in selectedPlayers)
                    if (player != null && !player.isDeleted)
                        newPlayers[i++] = player;
                group.selectedPlayers = newPlayers;
            }

            group.dataName = toRestore.dataName;
            group.isGlobal = toRestore.isGlobal;
            group.owningPlayer = toRestore.owningPlayer;
            selectionManager.SendOverwriteIA(group, isUndo: true);
            selectionManager.PopFromOverwriteUndoStack();

            // Give some kind of instant feedback, since this is not latency hidden.
            undoOverwriteLabel.text = "Undoing...";
            resetUndoOverwriteLabelTextCount++;
            SendCustomEventDelayedSeconds(nameof(ResetUndoOverwriteLabelText), 0.25f);
        }

        private int resetUndoOverwriteLabelTextCount;
        public void ResetUndoOverwriteLabelText()
        {
            if (--resetUndoOverwriteLabelTextCount != 0)
                return;
            undoOverwriteLabel.text = undoOverwriteLabelNormalText;
        }

        private string GetFirstUnusedLocalGroupName()
        {
            return selectionManager.GetFirstUnusedDataName(
                selectionManager.LocalPlayer.localDynamicDataByName,
                "Group",
                alwaysUsePostfix: true);
        }

        private void PreviewGroupNameInPlaceholder()
        {
            // TODO: Update in delete event too.
            groupNameFieldPlaceholder.text = GetFirstUnusedLocalGroupName();
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupAdded)]
        public void OnSelectionGroupAdded()
        {
            OnDynamicDataAdded(selectionManager.SelectionGroupForEvent);
            if (isHoveringLocalSaveButton)
                PreviewGroupNameInPlaceholder();
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupPlayerRemoved)]
        public void OnSelectionGroupPlayerRemoved()
        {
            if (TryGetDynamicDataButton(selectionManager.SelectionGroupForEvent.id, out DynamicDataOverwriteButton button))
                UpdateDynamicDataButtonLabel(button);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupOverwritten)]
        public void OnSelectionGroupOverwritten()
        {
            OnDynamicDataOverwritten(selectionManager.SelectionGroupForEvent, selectionManager.OverwrittenSelectionGroupForEvent);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupUndoOverwriteStackChanged)]
        public void OnSelectionGroupUndoOverwriteStackChanged()
        {
            bool interactable = selectionManager.OverwriteUndoStackSize != 0;
            undoOverwriteButton.interactable = interactable;
            undoOverwriteLabelSelectable.interactable = interactable;
        }

        protected override string GetDynamicDataLabel(DynamicData data)
        {
            PlayerSelectionGroup group = (PlayerSelectionGroup)data;
            return $"{group.dataName}  ({group.selectedPlayers.Length})";
        }
    }
}
