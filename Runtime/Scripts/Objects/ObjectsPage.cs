using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsPage : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private ObjectsPageManagerAPI objectsPageManager;
        [HideInInspector][SerializeField][SingletonReference] private ObjectsFavoritesManager objectsFavoritesManager;

        public ObjectsList rowsList;
        public ObjectsRow rowPrefabScript;

        private ObjectsRow activeRow;

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

        [MenuManagerEvent(MenuManagerEventType.OnMenuActivePageChanged)]
        public void OnMenuActivePageChanged()
        {
            ClearActiveRow();
            // TODO: Close popups?
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
            // if (!lockstep.IsContinuationFromPrevFrame)
            //     EnsureClosedPopups();
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

        #region Highlight

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
                SetActiveRow(row);
            else if (row == activeRow)
                ClearActiveRow();
        }

        #endregion
    }
}
