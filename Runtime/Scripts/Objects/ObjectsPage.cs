using TMPro;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    public enum ObjectsPageMode
    {
        Idle,
        Creating,
        Editing,
        Deleting,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsPage : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private ObjectsPageManagerAPI objectsPageManager;
        [HideInInspector][SerializeField][SingletonReference] private ObjectsFavoritesManager objectsFavoritesManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        public ObjectsList rowsList;
        public ObjectsRow rowPrefabScript;

        public Transform popupsParent;

        private ObjectsPageMode currentMode;
        private RectTransform activePopup;

        #region Creating
        public RectTransform creatingPopup;
        public TextMeshProUGUI creatingHeaderLabel;
        private string creatingHeaderLabelFormat;
        private ObjectsRow activeRow;
        #endregion

        #region Editing
        public RectTransform editingPopup;
        #endregion

        #region Deleting
        public RectTransform deletingPopup;
        #endregion

        [PermissionDefinitionReference(nameof(useObjectsPDef))]
        public string useObjectsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition useObjectsPDef;

        [PermissionDefinitionReference(nameof(viewObjectCategoryPDef))]
        public string viewObjectCategoryPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewObjectCategoryPDef;

        // private bool isInitialized = false;

        private RPPlayerData localPlayer;

        [PlayerDataEvent(PlayerDataEventType.OnLocalPlayerDataAvailable)]
        public void OnLocalPlayerDataAvailable()
        {
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.LocalPlayerData);
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            creatingHeaderLabelFormat = creatingHeaderLabel.text;
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuActivePageChanged)]
        public void OnMenuActivePageChanged()
        {
            EnterIdleMode();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            // isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            // isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            EnterIdleMode();
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            if (playerDataManager.IsPartOfCurrentImport)
                UpdateAllFavorites();
        }

        #region PermissionResolution

        public override void InitializeInstantiated() { }

        public override void ResolveAll()
        {
            if (!useObjectsPDef.valueForLocalPlayer)
                EnterIdleMode();

            bool viewObjectCategoryValue = viewObjectCategoryPDef.valueForLocalPlayer;

            rowsList.SortOnPermissionChange(viewObjectCategoryValue);

            bool objectCategoryChanged = rowPrefabScript.categoryRoot.activeSelf != viewObjectCategoryValue;
            rowPrefabScript.categoryRoot.SetActive(viewObjectCategoryValue);

            if (!objectCategoryChanged)
                return;

            for (int i = 0; i < 2; i++)
            {
                ObjectsRow[] rows = i == 0 ? rowsList.Rows : rowsList.UnusedRows;
                int rowsCount = i == 0 ? rowsList.RowsCount : rowsList.UnusedRowsCount;
                for (int j = 0; j < rowsCount; j++)
                    rows[j].categoryRoot.SetActive(viewObjectCategoryValue);
            }
        }

        #endregion

        #region RowsManagement

        private bool TryGetRow(uint entityPrototypeId, out ObjectsRow row) => rowsList.TryGetRow(entityPrototypeId, out row);

        private void RebuildRows()
        {
            if (!lockstep.IsContinuationFromPrevFrame && currentMode == ObjectsPageMode.Creating)
                ExitCreatingMode(); // Creating relies on an active row.
            rowsList.RebuildRows();
        }

        #endregion

        #region Favorite

        public void OnFavoriteValueChanged(ObjectsRow row)
        {
            bool isFavorite = row.favoriteToggle.isOn;
            if (isFavorite)
                objectsFavoritesManager.SendAddFavoriteIA(localPlayer, row.entityPrototype);
            else
                objectsFavoritesManager.SendRemoveFavoriteIA(localPlayer, row.entityPrototype);
            // Latency hiding.
            row.isFavorite = isFavorite;
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        [ObjectsFavoritesEvent(ObjectsFavoritesEventType.OnObjectFavoriteAdded)]
        public void OnObjectFavoriteAdded() => OnObjectFavoriteChanged(true);

        [ObjectsFavoritesEvent(ObjectsFavoritesEventType.OnObjectFavoriteRemoved)]
        public void OnObjectFavoriteRemoved() => OnObjectFavoriteChanged(false);

        private void OnObjectFavoriteChanged(bool isFavorite)
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            if (!objectsFavoritesManager.PlayerForEvent.core.isLocal)
                return;
            if (!TryGetRow(objectsFavoritesManager.EntityPrototypeForEvent.Id, out ObjectsRow row))
                return;
            row.isFavorite = isFavorite;
            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        private void UpdateAllFavorites()
        {
            ObjectsRow[] rows = rowsList.Rows;
            int rowsCount = rowsList.RowsCount;
            bool anyChanged = false;
            for (int i = 0; i < rowsCount; i++)
            {
                ObjectsRow row = rows[i];
                bool isFavorite = localPlayer.favoriteObjectIdsLut.ContainsKey(row.entityPrototype.Id);
                if (row.isFavorite == isFavorite)
                    continue;
                row.isFavorite = isFavorite;
                row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
                anyChanged = true;
            }
            if (anyChanged)
                rowsList.SortAllRows();
        }

        #endregion

        #region Row Highlight

        private void ClearActiveRow()
        {
            if (activeRow == null)
                return;
            activeRow.highlightToggle.SetIsOnWithoutNotify(false);
            activeRow = null;
        }

        private void SetActiveRow(ObjectsRow row)
        {
            if (activeRow == row)
                return;
            ClearActiveRow();
            activeRow = row;
        }

        public void OnHighlightToggleValueChanged(ObjectsRow row)
        {
            if (row.highlightToggle.isOn)
                EnterCreatingMode(row);
            else if (row == activeRow)
                EnterIdleMode();
        }

        #endregion

        #region Modes

        private void EnterIdleMode()
        {
            switch (currentMode)
            {
                case ObjectsPageMode.Creating:
                    ExitCreatingMode();
                    break;
                case ObjectsPageMode.Editing:
                    ExitEditingMode();
                    break;
                case ObjectsPageMode.Deleting:
                    ExitDeletingMode();
                    break;
            }
        }

        #region Popups

        private void ShowPopup(RectTransform popup)
        {
            menuManager.ShowPopupAtItsAnchor(popup, this, nameof(OnPopupClosed));
            activePopup = popup;
        }

        private void CloseActivePopup()
        {
            if (activePopup == null)
                return;
            menuManager.ClosePopup(activePopup, doCallback: false);
            ReturnActivePopup();
        }

        public void OnPopupClosed()
        {
            ReturnActivePopup();
            EnterIdleMode();
        }

        private void ReturnActivePopup()
        {
            activePopup.SetParent(popupsParent, worldPositionStays: false);
            activePopup = null;
        }

        #endregion

        #region Creating

        private void EnterCreatingMode(ObjectsRow row)
        {
            currentMode = ObjectsPageMode.Creating;
            SetActiveRow(row);
            creatingHeaderLabel.text = string.Format(creatingHeaderLabelFormat, row.entityPrototype.DisplayName);
            ShowPopup(creatingPopup);
        }

        private void ExitCreatingMode()
        {
            currentMode = ObjectsPageMode.Idle;
            ClearActiveRow();
            CloseActivePopup();
        }

        #endregion

        #region Editing

        public void OnEditExistingClick() => EnterEditingMode();

        private void EnterEditingMode()
        {
            currentMode = ObjectsPageMode.Editing;
            ShowPopup(editingPopup);
        }

        private void ExitEditingMode()
        {
            currentMode = ObjectsPageMode.Idle;
            CloseActivePopup();
        }

        #endregion

        #region Deleting

        public void OnDeleteExistingClick() => EnterDeletingMode();

        private void EnterDeletingMode()
        {
            currentMode = ObjectsPageMode.Deleting;
            ShowPopup(deletingPopup);
        }

        private void ExitDeletingMode()
        {
            currentMode = ObjectsPageMode.Idle;
            CloseActivePopup();
        }

        #endregion

        #endregion
    }
}
