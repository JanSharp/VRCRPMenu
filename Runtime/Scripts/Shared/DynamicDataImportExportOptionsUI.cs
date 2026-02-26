using UnityEngine;

namespace JanSharp
{
    public abstract class DynamicDataImportExportOptionsUI : LockstepGameStateOptionsUI
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataExportUIAPI playerDataExportUI;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataImportUIAPI playerDataImportUI;

        public override string OptionsClassName => nameof(DynamicDataImportExportOptions);

        protected abstract string ToggleLabel { get; }

        [SerializeField] private bool isImportUI;

        private DynamicDataImportExportOptions currentOptions;
        private DynamicDataImportExportOptions optionsToValidate;

        private ToggleFieldWidgetData includeGlobalToggle;
        private ToggleFieldWidgetData includePerPlayerToggle;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<DynamicDataImportExportOptions>(nameof(DynamicDataImportExportOptions));
        }

        protected override void ValidateOptionsImpl()
        {
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
            if (includeGlobalToggle.Interactable)
                currentOptions.includeGlobal = includeGlobalToggle.Value;
            if (includePerPlayerToggle.Interactable)
                currentOptions.includePerPlayer = includePerPlayerToggle.Value;
        }

        protected override void InitWidgetData()
        {
        }

        private void LazyInitWidgetData()
        {
            if (includeGlobalToggle != null)
                return;
            includeGlobalToggle = widgetManager.NewToggleField($"Global {ToggleLabel}", false);
            includePerPlayerToggle = widgetManager.NewToggleField($"Local {ToggleLabel}", false);
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            LazyInitWidgetData();
            if (isImportUI)
            {
                var optionsFromExport = (DynamicDataImportExportOptions)lockstep.ReadCustomClass(nameof(DynamicDataImportExportOptions), isImport: true);
                includeGlobalToggle.Interactable = optionsFromExport.includeGlobal;
                includePerPlayerToggle.Interactable = optionsFromExport.includePerPlayer;
                optionsFromExport.Delete();
            }
            includeGlobalToggle.SetValueWithoutNotify(includeGlobalToggle.Interactable && currentOptions.includeGlobal);
            includePerPlayerToggle.SetValueWithoutNotify(includePerPlayerToggle.Interactable && currentOptions.includePerPlayer);
            ui.General.AddChildDynamic(includeGlobalToggle);
            AddPlayerDataToggle(includePerPlayerToggle);
        }

        private void AddPlayerDataToggle(ToggleFieldWidgetData toggle)
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
