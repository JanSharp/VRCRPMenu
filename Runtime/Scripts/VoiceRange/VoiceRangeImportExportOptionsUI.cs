using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeImportExportOptionsUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(VoiceRangeImportExportOptions);

        [SerializeField] private bool isImportUI;

        private VoiceRangeImportExportOptions currentOptions;
        private VoiceRangeImportExportOptions optionsToValidate;

        private ToggleFieldWidgetData includeVoiceRangeSettingsToggle;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<VoiceRangeImportExportOptions>(nameof(VoiceRangeImportExportOptions));
        }

        protected override void ValidateOptionsImpl()
        {
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
            if (includeVoiceRangeSettingsToggle.Interactable)
                currentOptions.includeVoiceRangeSettings = includeVoiceRangeSettingsToggle.Value;
        }

        protected override void InitWidgetData()
        {
        }

        private void LazyInitWidgetData()
        {
            if (includeVoiceRangeSettingsToggle != null)
                return;
            includeVoiceRangeSettingsToggle = widgetManager.NewToggleField("Voice Range Settings", false);
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            LazyInitWidgetData();
            if (isImportUI)
            {
                var optionsFromExport = (VoiceRangeImportExportOptions)lockstep.ReadCustomClass(nameof(VoiceRangeImportExportOptions), isImport: true);
                includeVoiceRangeSettingsToggle.Interactable = optionsFromExport.includeVoiceRangeSettings;
                optionsFromExport.Delete();
            }
            includeVoiceRangeSettingsToggle.SetValueWithoutNotify(includeVoiceRangeSettingsToggle.Interactable && currentOptions.includeVoiceRangeSettings);
            ui.General.AddChildDynamic(includeVoiceRangeSettingsToggle);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
