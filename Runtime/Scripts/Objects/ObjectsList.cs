using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

// NOTE: This file is currently almost 100% copy paste from the ItemsList.
// With "item" replaced with "object" and "Item" replaced with "Object".
// And some rows related to the per row overlay in the create function removed.

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsList : SortableScrollableSearchableList
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private ObjectsPageManagerAPI objectsPageManager;

        public Image sortObjectNameAscendingImage;
        public Image sortObjectNameDescendingImage;
        public Image sortCategoryAscendingImage;
        public Image sortCategoryDescendingImage;

        /// <summary>
        /// <para><see cref="uint"/> entityPrototypeId => <see cref="ObjectsRow"/> row</para>
        /// </summary>
        private DataDictionary rowsByPrototypeId = new DataDictionary();
        public ObjectsRow[] Rows => (ObjectsRow[])rows;
        public int RowsCount => rowsCount;
        public ObjectsRow[] HiddenRows => (ObjectsRow[])hiddenRows;
        public int HiddenRowsCount => hiddenRowsCount;
        public ObjectsRow[] UnusedRows => (ObjectsRow[])unusedRows;
        public int UnusedRowsCount => unusedRowsCount;

        private RPPlayerData localPlayer;

        public override void Initialize()
        {
            base.Initialize();

            currentSortOrderFunction = nameof(CompareRowObjectNameAscending);
            currentSortOrderImage = sortObjectNameAscendingImage;
            currentSortOrderImage.enabled = true;
            someRowsAreOutOfSortOrder = false;
        }

        [PlayerDataEvent(PlayerDataEventType.OnLocalPlayerDataAvailable)]
        public void OnLocalPlayerDataAvailable()
        {
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.LocalPlayerData);
        }

        #region RowsManagement

        public bool TryGetRow(uint entityPrototypeId, out ObjectsRow row)
        {
            if (rowsByPrototypeId.TryGetValue(entityPrototypeId, out DataToken rowToken))
            {
                row = (ObjectsRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        public ObjectsRow CreateRow(EntityPrototype prototype)
        {
            ObjectsRow row = CreateRowForPrototype(prototype);
            rowsByPrototypeId.Add(prototype.Id, row);
            InsertSortNewRow(row);
            return row;
        }

        public void RemoveRow(ObjectsRow row)
        {
            rowsByPrototypeId.Remove(row.entityPrototype.Id);
            RemoveRow((SortableScrollableRow)row);
        }

        public void RebuildRows() => RebuildRows(objectsPageManager.ObjectPrototypesCount);

        protected override void OnRowCreated(SortableScrollableRow row) { }

        protected override void OnPreRebuildRows()
        {
            rowsByPrototypeId.Clear();
        }

        protected override SortableScrollableRow RebuildRow(int index)
        {
            EntityPrototype prototype = objectsPageManager.GetObjectPrototype(index);
            ObjectsRow row = CreateRowForPrototype(prototype);
            rowsByPrototypeId.Add(prototype.Id, row);
            return row;
        }

        private ObjectsRow CreateRowForPrototype(EntityPrototype prototype)
        {
            ObjectsRow row = (ObjectsRow)CreateRow();
            row.entityPrototype = prototype;

            bool isFavorite = localPlayer.favoriteObjectIdsLut.ContainsKey(prototype.Id);
            string objectName = prototype.DisplayName;
            string category = "Category"; // TODO

            row.isFavorite = isFavorite;
            row.sortableObjectName = objectName.ToLower();
            row.sortableCategory = category.ToLower();

            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            row.categoryLabel.text = category;
            row.highlightToggle.SetIsOnWithoutNotify(false);

            UpdateNewlyCreatedRow(row);

            return row;
        }

        #endregion

        #region SortHeaders

        // NOTE: Cannot just invert the order of the rows when inverting the order of a sorted column.
        // The categories are the most clear example of this. When inverting the sort order there it makes
        // more sense for just the categories to flip order, while objects in those categories retain relative
        // order

        public void OnObjectNameSortHeaderClick()
        {
            if (currentSortOrderImage != null)
                currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowObjectNameAscending))
            {
                currentSortOrderFunction = nameof(CompareRowObjectNameDescending);
                currentSortOrderImage = sortObjectNameDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowObjectNameAscending);
                currentSortOrderImage = sortObjectNameAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        public void OnCategorySortHeaderClick()
        {
            if (currentSortOrderImage != null)
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

        public void SortOnPermissionChange(bool viewObjectCategoryValue)
        {
            if (!viewObjectCategoryValue
                && (currentSortOrderFunction == nameof(CompareRowCategoryAscending)
                    || currentSortOrderFunction == nameof(CompareRowCategoryDescending)))
            {
                currentSortOrderFunction = nameof(CompareRowObjectNameAscending);
                // No need for null check, it's only null while using CompareRowSearchResults.
                currentSortOrderImage.enabled = false;
                currentSortOrderImage = sortObjectNameAscendingImage;
                currentSortOrderImage.enabled = true;
                SortAll();
            }
        }

        public void PotentiallySortChangedFavoriteRow(ObjectsRow row)
        {
            UpdateSortPositionDueToValueChange(row);
        }

        public void SortAllRows()
        {
            SortAll();
        }

        #endregion

        #region MergeSortComparators

        public void CompareRowObjectNameAscending()
        {
            ObjectsRow left = (ObjectsRow)compareLeft;
            ObjectsRow right = (ObjectsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableObjectName
                    .CompareTo(right.sortableObjectName) <= 0;
        }
        public void CompareRowObjectNameDescending()
        {
            ObjectsRow left = (ObjectsRow)compareLeft;
            ObjectsRow right = (ObjectsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableObjectName
                    .CompareTo(right.sortableObjectName) >= 0;
        }

        public void CompareRowCategoryAscending()
        {
            ObjectsRow left = (ObjectsRow)compareLeft;
            ObjectsRow right = (ObjectsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableCategory
                    .CompareTo(right.sortableCategory) <= 0;
        }
        public void CompareRowCategoryDescending()
        {
            ObjectsRow left = (ObjectsRow)compareLeft;
            ObjectsRow right = (ObjectsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableCategory
                    .CompareTo(right.sortableCategory) >= 0;
        }

        #endregion
    }
}
