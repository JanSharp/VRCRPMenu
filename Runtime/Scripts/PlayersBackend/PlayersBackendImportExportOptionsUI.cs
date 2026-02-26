using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendImportExportOptionsUI : LockstepGameStateOptionsUI
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataExportUIAPI playerDataExportUI;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataImportUIAPI playerDataImportUI;

        public override string OptionsClassName => nameof(PlayersBackendImportExportOptions);

        [SerializeField] private bool isImportUI;

        private PlayersBackendImportExportOptions currentOptions;
        private PlayersBackendImportExportOptions optionsToValidate;

        private ToggleFieldWidgetData includeOverriddenDisplayNameToggle;
        private ToggleFieldWidgetData includeCharacterNameToggle;
        private ToggleFieldWidgetData includeFavoriteItemsToggle;
        private ToggleFieldWidgetData includeFavoritePlayersToggle;

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
            if (includeFavoritePlayersToggle.Interactable)
                currentOptions.includeFavoritePlayers = includeFavoritePlayersToggle.Value;
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
            includeFavoritePlayersToggle = widgetManager.NewToggleField("Favorite Players", false);
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
                includeFavoritePlayersToggle.Interactable = optionsFromExport.includeFavoritePlayers;
                optionsFromExport.Delete();
            }
            includeOverriddenDisplayNameToggle.SetValueWithoutNotify(includeOverriddenDisplayNameToggle.Interactable && currentOptions.includeOverriddenDisplayName);
            includeCharacterNameToggle.SetValueWithoutNotify(includeCharacterNameToggle.Interactable && currentOptions.includeCharacterName);
            includeFavoriteItemsToggle.SetValueWithoutNotify(includeFavoriteItemsToggle.Interactable && currentOptions.includeFavoriteItems);
            includeFavoritePlayersToggle.SetValueWithoutNotify(includeFavoritePlayersToggle.Interactable && currentOptions.includeFavoritePlayers);
            AddToggle(includeOverriddenDisplayNameToggle);
            AddToggle(includeCharacterNameToggle);
            AddToggle(includeFavoriteItemsToggle);
            AddToggle(includeFavoritePlayersToggle);
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
