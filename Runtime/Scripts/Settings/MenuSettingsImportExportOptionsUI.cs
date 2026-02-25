using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuSettingsImportExportOptionsUI : LockstepGameStateOptionsUI
    {
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
            ui.General.AddChildDynamic(includeMenuSettingsToggle);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
