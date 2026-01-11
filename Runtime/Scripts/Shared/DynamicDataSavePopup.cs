using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace JanSharp
{
    public abstract class DynamicDataSavePopup : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] protected LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] protected PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][FindInParent] protected MenuManagerAPI menuManager;
        private DynamicDataManager dynamicDataManager;
        protected abstract DynamicDataManager GetDynamicDataManager();

        public RectTransform popupRoot;
        public RectTransform overwritePopupRect;
        public float headerHeight;
        public GameObject dynamicDataPrefab;
        public LayoutElement dynamicDataPrefabLayoutElement;
        public Button localButtonStyle;
        public TextMeshProUGUI localLabelStyle;
        public Button globalButtonStyle;
        public TextMeshProUGUI globalLabelStyle;
        public Transform dynamicDataParent;
        [Min(0)]
        public int dynamicDataButtonSiblingIndexBaseOffset;
        public ScrollRect dynamicDataScrollRect;
        private float dynamicDataButtonHeight;
        private float maxDynamicDataPopupHeight;

        public GameObject noDynamicDataInfoGo;
        public LayoutElement noDynamicDataInfoLayoutElement;
        private float noDynamicDataInfoHeight;

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

        /// <summary>
        /// <para><see cref="uint"/> dataId => <see cref="DynamicDataOverwriteButton"/> button</para>
        /// </summary>
        private DataDictionary buttonsById = new DataDictionary();
        private DynamicDataOverwriteButton[] buttons = new DynamicDataOverwriteButton[ArrList.MinCapacity];
        private int buttonsCount = 0;
        private DynamicDataOverwriteButton[] unusedButtons = new DynamicDataOverwriteButton[ArrList.MinCapacity];
        private int unusedButtonsCount = 0;

        private bool isInitialized = false;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            dynamicDataManager = GetDynamicDataManager();
            dynamicDataButtonHeight = dynamicDataPrefabLayoutElement.preferredHeight;
            maxDynamicDataPopupHeight = overwritePopupRect.sizeDelta.y;
            noDynamicDataInfoHeight = noDynamicDataInfoLayoutElement.preferredHeight;
            dataNameFieldPlaceholderNormalText = dataNameFieldPlaceholder.text;
            undoOverwriteLabelNormalText = undoOverwriteLabel.text;
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            RebuildDynamicDataButtons();
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            RebuildDynamicDataButtons();
            isInitialized = true;
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (!isInitialized)
                return;
            RebuildDynamicDataButtons();
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
            // TODO: Update in delete event too.
            dataNameFieldPlaceholder.text = GetFirstUnusedLocalDataName();
        }

        protected bool TryGetDynamicDataButton(uint dataId, out DynamicDataOverwriteButton button)
        {
            if (buttonsById.TryGetValue(dataId, out DataToken buttonToken))
            {
                button = (DynamicDataOverwriteButton)buttonToken.Reference;
                return true;
            }
            button = null;
            return false;
        }

        protected void OnDynamicDataOverwritten(DynamicData data, DynamicData overwrittenData)
        {
            if (!buttonsById.Remove(overwrittenData.id, out DataToken buttonToken))
                return; // Should be impossible.
            DynamicDataOverwriteButton button = (DynamicDataOverwriteButton)buttonToken.Reference;
            button.dynamicData = data;
            buttonsById.Add(data.id, button);
            // No need to set sortableDataName because overwritten data has the same name.
            UpdateDynamicDataButtonLabel(button); // But the label could differ even with the same name.
        }

        protected void OnDynamicDataUndoOverwriteStackChanged()
        {
            bool interactable = dynamicDataManager.OverwriteUndoStackSize != 0;
            undoOverwriteButton.interactable = interactable;
            undoOverwriteLabelSelectable.interactable = interactable;
        }

        protected void OnDynamicDataAdded(DynamicData data)
        {
            if (isHoveringLocalSaveButton)
                PreviewDataNameInPlaceholder();

            // if (data.isDeleted)
            //     return;
            if (!data.isGlobal && !data.owningPlayer.isLocal)
                return;
            if (data.isGlobal ? !globalOverwritePDef.valueForLocalPlayer : !localOverwritePDef.valueForLocalPlayer)
                return;
            DynamicDataOverwriteButton button = CreateDynamicDataButton(data);
            buttonsById.Add(data.id, button);
            InsertSortDynamicDataButton(button);
            UpdateDueToChangedButtonCount();
        }

        protected void OnDynamicDataDeleted(DynamicData data)
        {
            if (!TryGetDynamicDataButton(data.id, out DynamicDataOverwriteButton button))
                return;
            button.gameObject.SetActive(false);
            button.transform.SetAsLastSibling();
            ArrList.Add(ref unusedButtons, ref unusedButtonsCount, button);
            ArrList.Remove(ref buttons, ref buttonsCount, button);
            UpdateDueToChangedButtonCount();
        }

        private void RebuildDynamicDataButtons()
        {
            PerPlayerDynamicData localPlayer = dynamicDataManager.GetPlayerData(playerDataManager.LocalPlayerData);
            int newCount = 0;
            if (localOverwritePDef.valueForLocalPlayer)
                newCount += localPlayer.localDynamicDataCount;
            if (globalOverwritePDef.valueForLocalPlayer)
                newCount += dynamicDataManager.globalDynamicDataCount;

            buttonsById.Clear();
            ArrList.AddRange(ref unusedButtons, ref unusedButtonsCount, buttons, buttonsCount);
            for (int i = 0; i < buttonsCount - newCount; i++)
            {
                // Disable the low index ones, the higher ones will be reused from the unusedRows "stack".
                DynamicDataOverwriteButton button = buttons[i];
                button.gameObject.SetActive(false);
                button.transform.SetAsLastSibling();
            }

            ArrList.Clear(ref buttons, ref buttonsCount);
            ArrList.EnsureCapacity(ref buttons, newCount);

            if (localOverwritePDef.valueForLocalPlayer)
            {
                DynamicData[] dataList = localPlayer.localDynamicData;
                int dataCount = localPlayer.localDynamicDataCount;
                for (int i = 0; i < dataCount; i++)
                {
                    DynamicData data = dataList[i];
                    DynamicDataOverwriteButton button = CreateDynamicDataButton(data);
                    InsertSortDynamicDataButton(button);
                    buttonsById.Add(data.id, button);
                }
            }

            if (globalOverwritePDef.valueForLocalPlayer)
            {
                DynamicData[] dataList = dynamicDataManager.globalDynamicData;
                int dataCount = dynamicDataManager.globalDynamicDataCount;
                for (int i = 0; i < dataCount; i++)
                {
                    DynamicData data = dataList[i];
                    DynamicDataOverwriteButton button = CreateDynamicDataButton(data);
                    InsertSortDynamicDataButton(button);
                    buttonsById.Add(data.id, button);
                }
            }

            UpdateDueToChangedButtonCount();
        }

        private string GetSortableDataName(DynamicData data)
        {
            return (data.isGlobal ? "a" : "b") + data.dataName.ToLower();
        }

        private DynamicDataOverwriteButton CreateDynamicDataButton(DynamicData data)
        {
            DynamicDataOverwriteButton button = CreateDynamicDataButton();
            button.dynamicData = data;
            button.sortableDataName = GetSortableDataName(data);
            bool isGlobal = data.isGlobal;
            button.button.colors = isGlobal ? globalButtonStyle.colors : localButtonStyle.colors;
            button.label.color = isGlobal ? globalLabelStyle.color : localLabelStyle.color;
            UpdateDynamicDataButtonLabel(button);

            button.gameObject.SetActive(true);
            return button;
        }

        private DynamicDataOverwriteButton CreateDynamicDataButton()
        {
            if (unusedButtonsCount != 0)
                return ArrList.RemoveAt(ref unusedButtons, ref unusedButtonsCount, unusedButtonsCount - 1);
            GameObject go = Instantiate(dynamicDataPrefab);
            go.transform.SetParent(dynamicDataParent, worldPositionStays: false);
            return go.GetComponent<DynamicDataOverwriteButton>();
        }

        protected virtual string GetDynamicDataLabel(DynamicData data)
        {
            return data.isGlobal ? data.dataName + " [G]" : data.dataName;
        }

        protected void UpdateDynamicDataButtonLabel(DynamicDataOverwriteButton button)
        {
            button.label.text = GetDynamicDataLabel(button.dynamicData);
        }

        private void InsertSortDynamicDataButton(DynamicDataOverwriteButton button)
        {
            if (buttonsCount == 0)
            {
                button.transform.SetSiblingIndex(dynamicDataButtonSiblingIndexBaseOffset);
                ArrList.Add(ref buttons, ref buttonsCount, button);
                return;
            }
            string sortableDataName = button.sortableDataName;
            uint id = button.dynamicData.id;
            int index = buttonsCount; // Not -1 because the new row is not in the list yet.
            do
            {
                DynamicDataOverwriteButton leftButton = buttons[index - 1];
                int compared = leftButton.sortableDataName.CompareTo(sortableDataName);
                if (compared < 0 || compared == 0 && leftButton.dynamicData.id < id)
                    break;
                index--;
            }
            while (index > 0);
            button.transform.SetSiblingIndex(dynamicDataButtonSiblingIndexBaseOffset + index);
            ArrList.Insert(ref buttons, ref buttonsCount, button, index);
        }

        private void UpdateDueToChangedButtonCount()
        {
            ShowHideNoDynamicDataInfo();
            CalculateDynamicDataPopupHeight();
        }

        private void ShowHideNoDynamicDataInfo()
        {
            noDynamicDataInfoGo.SetActive(buttonsCount == 0);
        }

        private void CalculateDynamicDataPopupHeight()
        {
            float desiredHeight = buttonsCount == 0
                ? headerHeight + noDynamicDataInfoHeight
                : headerHeight + buttonsCount * dynamicDataButtonHeight;
            Vector2 sizeDelta = overwritePopupRect.sizeDelta;
            sizeDelta.y = Mathf.Min(maxDynamicDataPopupHeight, desiredHeight);
            overwritePopupRect.sizeDelta = sizeDelta;
            dynamicDataScrollRect.vertical = desiredHeight > maxDynamicDataPopupHeight;
        }
    }
}
