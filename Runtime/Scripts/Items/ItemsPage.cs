using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemsPage : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemsPageManagerAPI itemsPageManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemsFavoritesManagerAPI itemsFavoritesManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemSpawnLocationHelperAPI itemSpawnLocationHelper;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;

        public ItemsList rowsList;
        public ItemsRow rowPrefabScript;
        public ToggleGroupWithFloatValues itemSizeToggles;

        private ItemsRow activeRow;

        [PermissionDefinitionReference(nameof(viewItemCategoryPDef))]
        public string viewItemCategoryPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewItemCategoryPDef;

        // private bool isInitialized = false;

        /// <summary>
        /// <para>Check <see langword="null"/> and call <see cref="FetchLocalPlayer"/> before using.</para>
        /// </summary>
        private RPPlayerData localPlayer;

        /// <summary>
        /// <para>To avoid having to do this using OnInit would be required, which makes putting a rebuild
        /// rows call into OnInit and checking isInitialized in every event handler.</para>
        /// </summary>
        private void FetchLocalPlayer()
        {
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.LocalPlayerData);
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

        public override void Resolve()
        {
            bool itemCategoryValue = viewItemCategoryPDef.valueForLocalPlayer;

            rowsList.SortOnPermissionChange(itemCategoryValue);

            bool itemCategoryChanged = rowPrefabScript.categoryRoot.activeSelf != itemCategoryValue;
            rowPrefabScript.categoryRoot.SetActive(itemCategoryValue);

            if (!itemCategoryChanged)
                return;

            for (int i = 0; i < 2; i++)
            {
                ItemsRow[] rows = i == 0 ? rowsList.Rows : rowsList.UnusedRows;
                int rowsCount = i == 0 ? rowsList.RowsCount : rowsList.UnusedRowsCount;
                for (int j = 0; j < rowsCount; j++)
                    rows[j].categoryRoot.SetActive(itemCategoryValue);
            }
        }

        #endregion

        #region RowsManagement

        private bool TryGetRow(uint entityPrototypeId, out ItemsRow row) => rowsList.TryGetRow(entityPrototypeId, out row);

        private void RebuildRows()
        {
            // if (!lockstep.IsContinuationFromPrevFrame)
            //     EnsureClosedPopups();
            rowsList.RebuildRows();
        }

        #endregion

        #region Favorite

        public void OnFavoriteValueChanged(ItemsRow row)
        {
            if (localPlayer == null)
                FetchLocalPlayer();
            bool isFavorite = row.favoriteToggle.isOn;
            if (isFavorite)
                itemsFavoritesManager.SendAddFavoriteItemIA(localPlayer, row.entityPrototype);
            else
                itemsFavoritesManager.SendRemoveFavoriteItemIA(localPlayer, row.entityPrototype);
            // Latency hiding.
            row.isFavorite = isFavorite;
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        [ItemsFavoritesEvent(ItemsFavoritesEventType.OnItemFavoriteAdded)]
        public void OnItemFavoriteAdded() => OnItemFavoriteChanged(true);

        [ItemsFavoritesEvent(ItemsFavoritesEventType.OnItemFavoriteRemoved)]
        public void OnItemFavoriteRemoved() => OnItemFavoriteChanged(false);

        private void OnItemFavoriteChanged(bool isFavorite)
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            if (!itemsFavoritesManager.PlayerForEvent.core.isLocal)
                return;
            if (!TryGetRow(itemsFavoritesManager.EntityPrototypeForEvent.Id, out ItemsRow row))
                return;
            row.isFavorite = isFavorite;
            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        private void UpdateAllFavorites()
        {
            if (localPlayer == null)
                FetchLocalPlayer();
            ItemsRow[] rows = rowsList.Rows;
            int rowsCount = rowsList.RowsCount;
            bool anyChanged = false;
            for (int i = 0; i < rowsCount; i++)
            {
                ItemsRow row = rows[i];
                bool isFavorite = localPlayer.favoriteItemIdsLut.ContainsKey(row.entityPrototype.Id);
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

        #region Overlay

        private void ClearActiveRow()
        {
            if (activeRow == null)
                return;
            activeRow.spawnToggle.SetIsOnWithoutNotify(false);
            activeRow.itemNameLabelSelectable.interactable = true;
            activeRow.categoryLabelSelectable.interactable = true;
            activeRow.overlayRoot.SetActive(false);
            activeRow = null;
        }

        private void SetActiveRow(ItemsRow row)
        {
            if (activeRow == row)
                return;
            ClearActiveRow();
            activeRow = row;
            activeRow.itemNameLabelSelectable.interactable = false;
            activeRow.categoryLabelSelectable.interactable = false;
            activeRow.overlayRoot.SetActive(true);
        }

        public void OnSpawnToggleValueChanged(ItemsRow row)
        {
            if (row.spawnToggle.isOn)
                SetActiveRow(row);
            else if (row == activeRow)
                ClearActiveRow();
        }

        public void OnConfirmSpawnClick(ItemsRow row)
        {
            itemSpawnLocationHelper.DetermineItemSpawnLocation(this, nameof(OnSpawnLocationDetermined), new object[]
            {
                activeRow.entityPrototype,
                itemSizeToggles.GetValue(),
            });
            ClearActiveRow();
        }

        public void OnCancelSpawnClick(ItemsRow row)
        {
            ClearActiveRow();
        }

        public void OnSpawnLocationDetermined()
        {
            object[] callbackData = (object[])itemSpawnLocationHelper.CallbackCustomData;
            itemsPageManager.CreateItem(
                prototype: (EntityPrototype)callbackData[0],
                itemSpawnLocationHelper.DeterminedPosition,
                itemSpawnLocationHelper.DeterminedRotation,
                scale: (float)callbackData[1]);
        }

        #endregion
    }
}
