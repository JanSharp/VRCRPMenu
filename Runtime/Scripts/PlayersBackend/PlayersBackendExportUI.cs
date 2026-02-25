using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendExportUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(PlayersBackendExportOptions);

        private PlayersBackendExportOptions currentOptions;
        private PlayersBackendExportOptions optionsToValidate;

        private ToggleFieldWidgetData includeOverriddenDisplayNameToggle;
        private ToggleFieldWidgetData includeCharacterNameToggle;
        private ToggleFieldWidgetData includeFavoriteItemsToggle;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            PlayersBackendExportOptions options = wannaBeClasses.New<PlayersBackendExportOptions>(nameof(PlayersBackendExportOptions));
            return options;
        }

        protected override void ValidateOptionsImpl()
        {
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
            currentOptions.includeOverriddenDisplayName = includeOverriddenDisplayNameToggle.Value;
            currentOptions.includeCharacterName = includeCharacterNameToggle.Value;
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
            includeOverriddenDisplayNameToggle.SetValueWithoutNotify(currentOptions.includeOverriddenDisplayName);
            includeCharacterNameToggle.SetValueWithoutNotify(currentOptions.includeCharacterName);
            includeFavoriteItemsToggle.SetValueWithoutNotify(currentOptions.includeFavoriteItems);
            ui.General.AddChildDynamic(includeOverriddenDisplayNameToggle);
            ui.General.AddChildDynamic(includeCharacterNameToggle);
            ui.General.AddChildDynamic(includeFavoriteItemsToggle);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
