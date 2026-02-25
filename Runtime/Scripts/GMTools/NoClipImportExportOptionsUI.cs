using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipImportExportOptionsUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(NoClipImportExportOptions);

        [SerializeField] private bool isImportUI;

        private NoClipImportExportOptions currentOptions;
        private NoClipImportExportOptions optionsToValidate;

        private ToggleFieldWidgetData includeNoClipSettingsToggle;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<NoClipImportExportOptions>(nameof(NoClipImportExportOptions));
        }

        protected override void ValidateOptionsImpl()
        {
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
            if (includeNoClipSettingsToggle.Interactable)
                currentOptions.includeNoClipSettings = includeNoClipSettingsToggle.Value;
        }

        protected override void InitWidgetData()
        {
        }

        private void LazyInitWidgetData()
        {
            if (includeNoClipSettingsToggle != null)
                return;
            includeNoClipSettingsToggle = widgetManager.NewToggleField("No Clip Settings", false);
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            LazyInitWidgetData();
            if (isImportUI)
            {
                var optionsFromExport = (NoClipImportExportOptions)lockstep.ReadCustomClass(nameof(NoClipImportExportOptions), isImport: true);
                includeNoClipSettingsToggle.Interactable = optionsFromExport.includeNoClipSettings;
                optionsFromExport.Delete();
            }
            includeNoClipSettingsToggle.SetValueWithoutNotify(includeNoClipSettingsToggle.Interactable && currentOptions.includeNoClipSettings);
            ui.General.AddChildDynamic(includeNoClipSettingsToggle);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
