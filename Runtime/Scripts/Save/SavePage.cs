using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    public enum SavePageTabType
    {
        None,
        Import,
        Export,
        Autosave,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SavePage : PermissionResolver
    {
        [SerializeField][HideInInspector][FindInParent] private MenuManagerAPI menuManager;
        [SerializeField][HideInInspector][FindInParent] private MenuPageRoot menuPageRoot;

        [SerializeField] private SavePageTab[] tabs;
        private DataDictionary tabsByTabType = new DataDictionary();
        private SavePageTab activeTab = null;
        private SavePageTabType activeTabType = SavePageTabType.None;

        private bool pageIsShown;
        private bool pageIsActive;

        private bool isInitialized;

        [PermissionDefinitionReference(nameof(importGameStatesPDef))]
        public string importGameStatesPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition importGameStatesPDef;

        [PermissionDefinitionReference(nameof(exportGameStatesPDef))]
        public string exportGameStatesPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition exportGameStatesPDef;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            foreach (SavePageTab tab in tabs)
                if (tab.tabType != SavePageTabType.None)
                    // With an implicit or explicit cast to DataToken it would attempt to retrieve int value as DataToken.
                    tabsByTabType.Add(new DataToken((int)tab.tabType), tab);
                else
                    Debug.LogError($"[RPMenu] Invalid SavePageTab Tab Type, "
                        + $"must not be {nameof(SavePageTabType.None)}.", tab);
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuActivePageChanged)]
        public void OnMenuActivePageChanged()
        {
            bool newValue = menuManager.ActivePage == menuPageRoot;
            UpdatePageIsShown(newValue);
            if (pageIsActive == newValue)
                return;
            pageIsActive = newValue;
            if (activeTabType != SavePageTabType.None)
                if (pageIsActive)
                    activeTab.OnPageBecameActive();
                else
                    activeTab.OnPageBecameInactive();
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuOpenStateChanged)]
        public void OnMenuOpenStateChanged() => UpdatePageIsShown(pageIsActive);

        private void UpdatePageIsShown(bool pageIsActive)
        {
            bool newValue = menuManager.IsMenuOpen && pageIsActive;
            if (pageIsShown == newValue)
                return;
            pageIsShown = newValue;
            if (activeTabType != SavePageTabType.None)
                if (pageIsShown)
                    activeTab.OnTabGotShown();
                else
                    activeTab.OnTabGotHidden();
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (!isInitialized)
                return;

            SavePageTabType activeTabType = activeTab == null ? SavePageTabType.None : activeTab.tabType;
            if (activeTabType == SavePageTabType.Import && importGameStatesPDef.valueForLocalPlayer)
                return;
            if ((activeTabType == SavePageTabType.Export || activeTabType == SavePageTabType.Autosave) && exportGameStatesPDef.valueForLocalPlayer)
                return;

            if (importGameStatesPDef.valueForLocalPlayer)
                SetActiveTab(SavePageTabType.Import);
            else if (exportGameStatesPDef.valueForLocalPlayer)
                SetActiveTab(SavePageTabType.Export);
            else
                SetActiveTab(SavePageTabType.None);
        }

        public void OnTabToggleValueChanged(SavePageTab tab)
        {
            if (tab.tabToggle.isOn)
                SetActiveTab(tab.tabType);
            else
                tab.tabToggle.SetIsOnWithoutNotify(true);
        }

        private void SetActiveTab(SavePageTabType activeTabType)
        {
            if (this.activeTabType == activeTabType)
                return;

            if (this.activeTabType != SavePageTabType.None)
            {
                if (pageIsShown)
                    activeTab.OnTabGotHidden();
                if (pageIsActive)
                    activeTab.OnPageBecameInactive();
                activeTab.OnTabBecameInactive();
            }

            this.activeTabType = activeTabType;
            if (activeTabType == SavePageTabType.None)
            {
                activeTab = null;
                return;
            }
            // With an implicit or explicit cast to DataToken it would attempt to retrieve int value as DataToken.
            activeTab = (SavePageTab)tabsByTabType[new DataToken((int)activeTabType)].Reference;
            activeTab.OnTabBecameActive();
            if (pageIsActive)
                activeTab.OnPageBecameActive();
            if (pageIsShown)
                activeTab.OnTabGotShown();
        }

        private void OnInitialized()
        {
            isInitialized = true;
            Resolve();
        }

        [LockstepEvent(LockstepEventType.OnInitFinished, Order = 10)] // Run after each tab's initialization.
        public void OnInitFinished() => OnInitialized();

        [LockstepEvent(LockstepEventType.OnPostClientBeginCatchUp, Order = 10)] // Run after each tab's initialization.
        public void OnPostClientBeginCatchUp() => OnInitialized();
    }
}
