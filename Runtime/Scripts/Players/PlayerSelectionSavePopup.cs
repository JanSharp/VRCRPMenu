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

        private bool isHoveringLocalSaveButton;

        public override void OnMenuManagerStart()
        {
            base.OnMenuManagerStart();
            groupNameFieldPlaceholderNormalText = groupNameFieldPlaceholder.text;
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

        protected override string GetDynamicDataLabel(DynamicData data)
        {
            PlayerSelectionGroup group = (PlayerSelectionGroup)data;
            return $"{group.dataName}  ({group.selectedPlayers.Length})";
        }
    }
}
