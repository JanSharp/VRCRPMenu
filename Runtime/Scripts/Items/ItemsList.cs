using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemsList : SortableScrollableList
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemsPageManagerAPI itemsPageManager;

        public Image sortItemNameAscendingImage;
        public Image sortItemNameDescendingImage;
        public Image sortCategoryAscendingImage;
        public Image sortCategoryDescendingImage;

        /// <summary>
        /// <para><see cref="uint"/> entityPrototypeId => <see cref="ItemsRow"/> row</para>
        /// </summary>
        private DataDictionary rowsByPrototypeId = new DataDictionary();
        public ItemsRow[] Rows => (ItemsRow[])rows;
        public int RowsCount => rowsCount;
        public ItemsRow[] UnusedRows => (ItemsRow[])unusedRows;
        public int UnusedRowsCount => unusedRowsCount;

        private RPPlayerData localPlayer;

        public override void Initialize()
        {
            base.Initialize();

            currentSortOrderFunction = nameof(CompareRowItemNameAscending);
            currentSortOrderImage = sortItemNameAscendingImage;
            currentSortOrderImage.enabled = true;
            someRowsAreOutOfSortOrder = false;
        }

        private void FetchLocalPlayer()
        {
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.LocalPlayerData);
        }

        #region RowsManagement

        public bool TryGetRow(uint entityPrototypeId, out ItemsRow row)
        {
            if (rowsByPrototypeId.TryGetValue(entityPrototypeId, out DataToken rowToken))
            {
                row = (ItemsRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        public ItemsRow CreateRow(EntityPrototype prototype)
        {
            ItemsRow row = CreateRowForPrototype(prototype);
            rowsByPrototypeId.Add(prototype.Id, row);
            InsertSortNewRow(row);
            return row;
        }

        public void RemoveRow(ItemsRow row)
        {
            rowsByPrototypeId.Remove(row.entityPrototype.Id);
            RemoveRow((SortableScrollableRow)row);
        }

        public void RebuildRows() => RebuildRows(itemsPageManager.ItemPrototypesCount);

        protected override void OnRowCreated(SortableScrollableRow row) { }

        protected override void OnPreRebuildRows()
        {
            rowsByPrototypeId.Clear();
        }

        protected override SortableScrollableRow RebuildRow(int index)
        {
            EntityPrototype prototype = itemsPageManager.GetItemPrototype(index);
            ItemsRow row = CreateRowForPrototype(prototype);
            rowsByPrototypeId.Add(prototype.Id, row);
            return row;
        }

        private ItemsRow CreateRowForPrototype(EntityPrototype prototype)
        {
            ItemsRow row = (ItemsRow)CreateRow();
            row.entityPrototype = prototype;

            if (localPlayer == null)
                FetchLocalPlayer();
            bool isFavorite = localPlayer.favoriteItemIdsLut.ContainsKey(prototype.Id);
            string itemName = prototype.DisplayName;
            string category = "Category"; // TODO

            row.isFavorite = isFavorite;
            row.sortableItemName = itemName.ToLower();
            row.sortableCategory = category.ToLower();

            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            row.itemNameLabel.text = itemName;
            row.categoryLabel.text = category;
            row.spawnToggle.SetIsOnWithoutNotify(false);
            row.itemNameLabelSelectable.interactable = true;
            row.categoryLabelSelectable.interactable = true;
            row.overlayRoot.SetActive(false);

            return row;
        }

        #endregion

        #region SortHeaders

        // NOTE: Cannot just invert the order of the rows when inverting the order of a sorted column.
        // The categories are the most clear example of this. When inverting the sort order there it makes
        // more sense for just the categories to flip order, while items in those categories retain relative
        // order

        public void OnItemNameSortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowItemNameAscending))
            {
                currentSortOrderFunction = nameof(CompareRowItemNameDescending);
                currentSortOrderImage = sortItemNameDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowItemNameAscending);
                currentSortOrderImage = sortItemNameAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        public void OnCategorySortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowCategoryAscending))
            {
                currentSortOrderFunction = nameof(CompareRowCategoryDescending);
                currentSortOrderImage = sortCategoryDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowCategoryAscending);
                currentSortOrderImage = sortCategoryAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        #endregion

        #region SortAPI

        public void SortOnPermissionChange(bool itemCategoryValue)
        {
            if (!itemCategoryValue
                && (currentSortOrderFunction == nameof(CompareRowCategoryAscending)
                    || currentSortOrderFunction == nameof(CompareRowCategoryDescending)))
            {
                currentSortOrderFunction = nameof(CompareRowItemNameAscending);
                currentSortOrderImage.enabled = false;
                currentSortOrderImage = sortItemNameAscendingImage;
                currentSortOrderImage.enabled = true;
                SortAll();
            }
        }

        public void PotentiallySortChangedFavoriteRow(ItemsRow row)
        {
            UpdateSortPositionDueToValueChange(row);
        }

        public void SortAllRows()
        {
            SortAll();
        }

        #endregion

        #region MergeSortComparators

        public void CompareRowItemNameAscending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableItemName
                    .CompareTo(right.sortableItemName) <= 0;
        }
        public void CompareRowItemNameDescending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableItemName
                    .CompareTo(right.sortableItemName) >= 0;
        }

        public void CompareRowCategoryAscending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableCategory
                    .CompareTo(right.sortableCategory) <= 0;
        }
        public void CompareRowCategoryDescending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableCategory
                    .CompareTo(right.sortableCategory) >= 0;
        }

        #endregion
    }
}
