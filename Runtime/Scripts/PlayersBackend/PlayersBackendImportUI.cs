using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendImportUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(PlayersBackendImportOptions);

        private PlayersBackendImportOptions currentOptions;
        private PlayersBackendImportOptions optionsToValidate;

        private ToggleFieldWidgetData includeOverriddenDisplayNameToggle;
        private ToggleFieldWidgetData includeCharacterNameToggle;
        private ToggleFieldWidgetData includeFavoriteItemsToggle;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            PlayersBackendImportOptions options = wannaBeClasses.New<PlayersBackendImportOptions>(nameof(PlayersBackendImportOptions));
            return options;
        }

        protected override void ValidateOptionsImpl()
        {
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
            if (includeOverriddenDisplayNameToggle.Interactable)
                currentOptions.includeOverriddenDisplayName = includeOverriddenDisplayNameToggle.Value;
            if (includeCharacterNameToggle.Interactable)
                currentOptions.includeCharacterName = includeCharacterNameToggle.Value;
            if (includeFavoriteItemsToggle.Interactable)
                currentOptions.includeFavoriteItems = includeFavoriteItemsToggle.Value;
        }

        protected override void InitWidgetData()
        {
        }

        private void LazyInitWidgetData()
        {
            if (includeOverriddenDisplayNameToggle != null)
                return;
            includeOverriddenDisplayNameToggle = widgetManager.NewToggleField("Overridden Display Name", false);
            includeCharacterNameToggle = widgetManager.NewToggleField("Character Name", false);
            includeFavoriteItemsToggle = widgetManager.NewToggleField("Favorite Items", false);
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            LazyInitWidgetData();
            var optionsFromExport = (PlayersBackendExportOptions)lockstep.ReadCustomClass(nameof(PlayersBackendExportOptions), isImport: true);
            includeOverriddenDisplayNameToggle.Interactable = optionsFromExport.includeOverriddenDisplayName;
            includeCharacterNameToggle.Interactable = optionsFromExport.includeCharacterName;
            includeFavoriteItemsToggle.Interactable = optionsFromExport.includeFavoriteItems;
            optionsFromExport.Delete();
            includeOverriddenDisplayNameToggle.SetValueWithoutNotify(optionsFromExport.includeOverriddenDisplayName && currentOptions.includeOverriddenDisplayName);
            includeCharacterNameToggle.SetValueWithoutNotify(optionsFromExport.includeCharacterName && currentOptions.includeCharacterName);
            includeFavoriteItemsToggle.SetValueWithoutNotify(optionsFromExport.includeFavoriteItems && currentOptions.includeFavoriteItems);
            ui.General.AddChildDynamic(includeOverriddenDisplayNameToggle);
            ui.General.AddChildDynamic(includeCharacterNameToggle);
            ui.General.AddChildDynamic(includeFavoriteItemsToggle);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
