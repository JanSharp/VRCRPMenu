using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace JanSharp
{
    public abstract class DynamicDataPopupList : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] protected LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] protected PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][FindInParent] protected MenuManagerAPI menuManager;
        protected DynamicDataManager dynamicDataManager;
        protected abstract DynamicDataManager GetDynamicDataManager();

        public RectTransform popupRoot;
        public RectTransform popupRectToResize;
        public float headerHeight;
        public GameObject dynamicDataPrefab;
        public LayoutElement dynamicDataPrefabLayoutElement;
        public Button localButtonStyle;
        public Button globalButtonStyle;
        public Transform dynamicDataParent;
        [Min(0)]
        public int dynamicDataButtonSiblingIndexBaseOffset;
        public ScrollRect dynamicDataScrollRect;
        private float dynamicDataButtonHeight;
        private float maxDynamicDataPopupHeight;

        public GameObject noDynamicDataInfoGo;
        public LayoutElement noDynamicDataInfoLayoutElement;
        private float noDynamicDataInfoHeight;

        /// <summary>
        /// <para><see cref="uint"/> dataId => <see cref="DynamicDataPopupListButton"/> button</para>
        /// </summary>
        private DataDictionary buttonsById = new DataDictionary();
        private DynamicDataPopupListButton[] buttons = new DynamicDataPopupListButton[ArrList.MinCapacity];
        private int buttonsCount = 0;
        private DynamicDataPopupListButton[] unusedButtons = new DynamicDataPopupListButton[ArrList.MinCapacity];
        private int unusedButtonsCount = 0;

        protected bool isInitialized = false;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public virtual void OnMenuManagerStart()
        {
            dynamicDataManager = GetDynamicDataManager();
            dynamicDataButtonHeight = dynamicDataPrefabLayoutElement.preferredHeight;
            maxDynamicDataPopupHeight = popupRectToResize.sizeDelta.y;
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

        protected bool TryGetDynamicDataButton(uint dataId, out DynamicDataPopupListButton button)
        {
            if (buttonsById.TryGetValue(dataId, out DataToken buttonToken))
            {
                button = (DynamicDataPopupListButton)buttonToken.Reference;
                return true;
            }
            button = null;
            return false;
        }

        protected void OnDynamicDataOverwritten(DynamicData data, DynamicData overwrittenData)
        {
            if (!buttonsById.Remove(overwrittenData.id, out DataToken buttonToken))
                return; // Should be impossible.
            DynamicDataPopupListButton button = (DynamicDataPopupListButton)buttonToken.Reference;
            button.dynamicData = data;
            buttonsById.Add(data.id, button);
            // No need to set sortableDataName because overwritten data has the same name.
            UpdateDynamicDataButtonLabel(button); // But the label could differ even with the same name.
        }

        protected abstract bool ShouldShowLocalButtons();

        protected abstract bool ShouldShowGlobalButtons();

        protected virtual void OnDynamicDataAdded(DynamicData data)
        {
            // if (data.isDeleted)
            //     return;
            if (!data.isGlobal && !data.owningPlayer.isLocal)
                return;
            if (data.isGlobal ? !ShouldShowLocalButtons() : !ShouldShowGlobalButtons())
                return;
            DynamicDataPopupListButton button = CreateDynamicDataButton(data);
            buttonsById.Add(data.id, button);
            InsertSortDynamicDataButton(button);
            UpdateDueToChangedButtonCount();
        }

        protected virtual void OnDynamicDataDeleted(DynamicData data)
        {
            if (!TryGetDynamicDataButton(data.id, out DynamicDataPopupListButton button))
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
            bool shouldShowLocal = ShouldShowLocalButtons();
            bool shouldShowGlobal = ShouldShowGlobalButtons();
            if (shouldShowLocal)
                newCount += localPlayer.localDynamicDataCount;
            if (shouldShowGlobal)
                newCount += dynamicDataManager.globalDynamicDataCount;

            buttonsById.Clear();
            ArrList.AddRange(ref unusedButtons, ref unusedButtonsCount, buttons, buttonsCount);
            for (int i = 0; i < buttonsCount - newCount; i++)
            {
                // Disable the low index ones, the higher ones will be reused from the unusedRows "stack".
                DynamicDataPopupListButton button = buttons[i];
                button.gameObject.SetActive(false);
                button.transform.SetAsLastSibling();
            }

            ArrList.Clear(ref buttons, ref buttonsCount);
            ArrList.EnsureCapacity(ref buttons, newCount);

            if (shouldShowLocal)
            {
                DynamicData[] dataList = localPlayer.localDynamicData;
                int dataCount = localPlayer.localDynamicDataCount;
                for (int i = 0; i < dataCount; i++)
                {
                    DynamicData data = dataList[i];
                    DynamicDataPopupListButton button = CreateDynamicDataButton(data);
                    InsertSortDynamicDataButton(button);
                    buttonsById.Add(data.id, button);
                }
            }

            if (shouldShowGlobal)
            {
                DynamicData[] dataList = dynamicDataManager.globalDynamicData;
                int dataCount = dynamicDataManager.globalDynamicDataCount;
                for (int i = 0; i < dataCount; i++)
                {
                    DynamicData data = dataList[i];
                    DynamicDataPopupListButton button = CreateDynamicDataButton(data);
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

        private DynamicDataPopupListButton CreateDynamicDataButton(DynamicData data)
        {
            DynamicDataPopupListButton button = CreateDynamicDataButton();
            button.dynamicData = data;
            button.sortableDataName = GetSortableDataName(data);
            bool isGlobal = data.isGlobal;
            button.button.colors = isGlobal ? globalButtonStyle.colors : localButtonStyle.colors;
            UpdateDynamicDataButtonLabel(button);
            OnDynamicDataButtonCreated(button, data);

            button.gameObject.SetActive(true);
            return button;
        }

        protected virtual void OnDynamicDataButtonCreated(DynamicDataPopupListButton button, DynamicData data) { }

        private DynamicDataPopupListButton CreateDynamicDataButton()
        {
            if (unusedButtonsCount != 0)
                return ArrList.RemoveAt(ref unusedButtons, ref unusedButtonsCount, unusedButtonsCount - 1);
            GameObject go = Instantiate(dynamicDataPrefab);
            go.transform.SetParent(dynamicDataParent, worldPositionStays: false);
            return go.GetComponent<DynamicDataPopupListButton>();
        }

        protected virtual string GetDynamicDataLabel(DynamicData data)
        {
            return data.isGlobal ? data.dataName + " [G]" : data.dataName;
        }

        protected void UpdateDynamicDataButtonLabel(DynamicDataPopupListButton button)
        {
            button.label.text = GetDynamicDataLabel(button.dynamicData);
        }

        private void InsertSortDynamicDataButton(DynamicDataPopupListButton button)
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
                DynamicDataPopupListButton leftButton = buttons[index - 1];
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
            Vector2 sizeDelta = popupRectToResize.sizeDelta;
            sizeDelta.y = Mathf.Min(maxDynamicDataPopupHeight, desiredHeight);
            popupRectToResize.sizeDelta = sizeDelta;
            dynamicDataScrollRect.vertical = desiredHeight > maxDynamicDataPopupHeight;
        }
    }
}
