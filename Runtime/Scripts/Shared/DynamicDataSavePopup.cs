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
        public virtual void OnMenuManagerStart()
        {
            dynamicDataManager = GetDynamicDataManager();
            dynamicDataButtonHeight = dynamicDataPrefabLayoutElement.preferredHeight;
            maxDynamicDataPopupHeight = overwritePopupRect.sizeDelta.y;
            noDynamicDataInfoHeight = noDynamicDataInfoLayoutElement.preferredHeight;
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

        protected void OnDynamicDataOverwritten(DynamicData data)
        {
            if (TryGetDynamicDataButton(data.id, out DynamicDataOverwriteButton button))
                UpdateDynamicDataButtonLabel(button);
        }

        protected void OnDynamicDataAdded(DynamicData data)
        {
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

        protected abstract string GetDynamicDataLabel(DynamicData data);

        protected void UpdateDynamicDataButtonLabel(DynamicDataOverwriteButton button)
        {
            DynamicData data = button.dynamicData;
            string label = GetDynamicDataLabel(data);
            button.label.text = data.isGlobal ? label + " [G]" : label;
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
