using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    public abstract class DynamicDataSavePopup : DynamicDataPopupList
    {
        public TextMeshProUGUI localLabelStyle;
        public TextMeshProUGUI globalLabelStyle;
        public Button saveAsGlobalButton;
        public Selectable saveAsGlobalLabel;
        public TMP_InputField dataNameField;
        public TextMeshProUGUI dataNameFieldPlaceholder;
        private string dataNameFieldPlaceholderNormalText;
        public Button undoOverwriteButton;
        public Selectable undoOverwriteLabelSelectable;
        public TextMeshProUGUI undoOverwriteLabel;
        private string undoOverwriteLabelNormalText;

        private bool isHoveringLocalSaveButton;

        [PermissionDefinitionReference(nameof(localOverwritePDef))]
        public string localOverwritePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localOverwritePDef;

        [PermissionDefinitionReference(nameof(globalOverwritePDef))]
        public string globalOverwritePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalOverwritePDef;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public override void OnMenuManagerStart()
        {
            base.OnMenuManagerStart();
            dataNameFieldPlaceholderNormalText = dataNameFieldPlaceholder.text;
            undoOverwriteLabelNormalText = undoOverwriteLabel.text;
        }

        public void OnDataNameValueChanged()
        {
            UpdateSaveAsGlobalInteractable();
        }

        private void UpdateSaveAsGlobalInteractable()
        {
            bool interactable = !string.IsNullOrWhiteSpace(dataNameField.text);
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

        public void OnSaveAsLocalPointerEnter()
        {
            isHoveringLocalSaveButton = true;
            PreviewDataNameInPlaceholder();
        }

        public void OnSaveAsLocalPointerExit()
        {
            isHoveringLocalSaveButton = false;
            dataNameFieldPlaceholder.text = dataNameFieldPlaceholderNormalText;
        }

        private void SendAddIA(bool isGlobal)
        {
            DynamicData data = dynamicDataManager.dataForSerialization;

            string dataName = dataNameField.text.Trim();
            dataNameField.SetTextWithoutNotify("");
            UpdateSaveAsGlobalInteractable();
            if (!isGlobal && dataName == "")
                dataName = GetFirstUnusedLocalDataName();
            data.dataName = dataName;

            data.isGlobal = isGlobal;
            data.owningPlayer = playerDataManager.LocalPlayerData;
            PopulateDataForAdd(data);
            dynamicDataManager.SendAddIA(data);
        }

        protected abstract void PopulateDataForAdd(DynamicData data);

        public void OnOverwriteButtonClick(DynamicDataOverwriteButton button)
        {
            menuManager.ClosePopup(popupRoot, doCallback: true);
            DynamicData toOverwrite = button.dynamicData;
            DynamicData data = dynamicDataManager.dataForSerialization;

            data.dataName = toOverwrite.dataName;
            data.isGlobal = toOverwrite.isGlobal;
            data.owningPlayer = playerDataManager.LocalPlayerData;
            PopulateDataForOverwrite(data, toOverwrite);
            dynamicDataManager.SendOverwriteIA(data, isUndo: false);
        }

        protected abstract void PopulateDataForOverwrite(DynamicData data, DynamicData toOverwrite);

        public void OnUndoOverwriteClick()
        {
            DynamicData toRestore = dynamicDataManager.GetTopFromOverwriteUndoStack();
            if (toRestore == null)
                return;
            DynamicData data = dynamicDataManager.dataForSerialization;

            data.dataName = toRestore.dataName;
            data.isGlobal = toRestore.isGlobal;
            data.owningPlayer = toRestore.owningPlayer;
            PopulateDataForUndoOverwrite(data, toRestore);
            dynamicDataManager.SendOverwriteIA(data, isUndo: true);
            dynamicDataManager.PopFromOverwriteUndoStack();

            // Give some kind of instant feedback, since this is not latency hidden.
            undoOverwriteLabel.text = "Undoing...";
            resetUndoOverwriteLabelTextCount++;
            SendCustomEventDelayedSeconds(nameof(ResetUndoOverwriteLabelText), 0.3f);
        }

        protected abstract void PopulateDataForUndoOverwrite(DynamicData data, DynamicData toRestore);

        private int resetUndoOverwriteLabelTextCount;
        public void ResetUndoOverwriteLabelText()
        {
            if (--resetUndoOverwriteLabelTextCount != 0)
                return;
            undoOverwriteLabel.text = undoOverwriteLabelNormalText;
        }

        private string GetFirstUnusedLocalDataName()
        {
            return dynamicDataManager.GetFirstUnusedDataName(
                dynamicDataManager.GetPlayerData(playerDataManager.LocalPlayerData).localDynamicDataByName,
                DefaultBaseDataName,
                alwaysUsePostfix: true);
        }

        protected abstract string DefaultBaseDataName { get; }

        private void PreviewDataNameInPlaceholder()
        {
            dataNameFieldPlaceholder.text = GetFirstUnusedLocalDataName();
        }

        protected void OnDynamicDataUndoOverwriteStackChanged()
        {
            bool interactable = dynamicDataManager.OverwriteUndoStackSize != 0;
            undoOverwriteButton.interactable = interactable;
            undoOverwriteLabelSelectable.interactable = interactable;
        }

        protected override bool ShouldShowLocalButtons() => localOverwritePDef.valueForLocalPlayer;

        protected override bool ShouldShowGlobalButtons() => globalOverwritePDef.valueForLocalPlayer;

        protected override void OnDynamicDataAdded(DynamicData data)
        {
            if (isHoveringLocalSaveButton)
                PreviewDataNameInPlaceholder();
            base.OnDynamicDataAdded(data);
        }

        protected override void OnDynamicDataDeleted(DynamicData data)
        {
            if (isHoveringLocalSaveButton)
                PreviewDataNameInPlaceholder();
            base.OnDynamicDataDeleted(data);
        }

        protected override void OnDynamicDataButtonCreated(DynamicDataPopupListButton button, DynamicData data)
        {
            button.label.color = data.isGlobal ? globalLabelStyle.color : localLabelStyle.color;
        }
    }
}
