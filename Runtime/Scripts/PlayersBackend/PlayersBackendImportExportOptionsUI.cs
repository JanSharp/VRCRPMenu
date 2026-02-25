using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendImportExportOptionsUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(PlayersBackendImportExportOptions);

        [SerializeField] private bool isImportUI;

        private PlayersBackendImportExportOptions currentOptions;
        private PlayersBackendImportExportOptions optionsToValidate;

        private ToggleFieldWidgetData includeOverriddenDisplayNameToggle;
        private ToggleFieldWidgetData includeCharacterNameToggle;
        private ToggleFieldWidgetData includeFavoriteItemsToggle;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<PlayersBackendImportExportOptions>(nameof(PlayersBackendImportExportOptions));
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
            if (isImportUI)
            {
                var optionsFromExport = (PlayersBackendImportExportOptions)lockstep.ReadCustomClass(nameof(PlayersBackendImportExportOptions), isImport: true);
                includeOverriddenDisplayNameToggle.Interactable = optionsFromExport.includeOverriddenDisplayName;
                includeCharacterNameToggle.Interactable = optionsFromExport.includeCharacterName;
                includeFavoriteItemsToggle.Interactable = optionsFromExport.includeFavoriteItems;
                optionsFromExport.Delete();
            }
            includeOverriddenDisplayNameToggle.SetValueWithoutNotify(includeOverriddenDisplayNameToggle.Interactable && currentOptions.includeOverriddenDisplayName);
            includeCharacterNameToggle.SetValueWithoutNotify(includeCharacterNameToggle.Interactable && currentOptions.includeCharacterName);
            includeFavoriteItemsToggle.SetValueWithoutNotify(includeFavoriteItemsToggle.Interactable && currentOptions.includeFavoriteItems);
            ui.General.AddChildDynamic(includeOverriddenDisplayNameToggle);
            ui.General.AddChildDynamic(includeCharacterNameToggle);
            ui.General.AddChildDynamic(includeFavoriteItemsToggle);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
