using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuSettingsImportExportOptionsUI : LockstepGameStateOptionsUI
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataExportUIAPI playerDataExportUI;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataImportUIAPI playerDataImportUI;

        public override string OptionsClassName => nameof(MenuSettingsImportExportOptions);

        [SerializeField] private bool isImportUI;

        private MenuSettingsImportExportOptions currentOptions;
        private MenuSettingsImportExportOptions optionsToValidate;

        private ToggleFieldWidgetData includeMenuSettingsToggle;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<MenuSettingsImportExportOptions>(nameof(MenuSettingsImportExportOptions));
        }

        protected override void ValidateOptionsImpl()
        {
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
            if (includeMenuSettingsToggle.Interactable)
                currentOptions.includeMenuSettings = includeMenuSettingsToggle.Value;
        }

        protected override void InitWidgetData()
        {
        }

        private void LazyInitWidgetData()
        {
            if (includeMenuSettingsToggle != null)
                return;
            includeMenuSettingsToggle = widgetManager.NewToggleField("Menu Settings", false);
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            LazyInitWidgetData();
            if (isImportUI)
            {
                var optionsFromExport = (MenuSettingsImportExportOptions)lockstep.ReadCustomClass(nameof(MenuSettingsImportExportOptions), isImport: true);
                includeMenuSettingsToggle.Interactable = optionsFromExport.includeMenuSettings;
                optionsFromExport.Delete();
            }
            includeMenuSettingsToggle.SetValueWithoutNotify(includeMenuSettingsToggle.Interactable && currentOptions.includeMenuSettings);
            AddToggle(includeMenuSettingsToggle);
        }

        private void AddToggle(ToggleFieldWidgetData toggle)
        {
            if (isImportUI)
                playerDataImportUI.AddPlayerDataOptionToggle(toggle);
            else
                playerDataExportUI.AddPlayerDataOptionToggle(toggle);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
