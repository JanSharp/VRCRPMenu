using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeImportExportOptionsUI : LockstepGameStateOptionsUI
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataExportUIAPI playerDataExportUI;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataImportUIAPI playerDataImportUI;

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
            AddToggle(includeVoiceRangeSettingsToggle);
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
