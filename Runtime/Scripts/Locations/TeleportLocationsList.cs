using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeleportLocationsList : SortableScrollableList
    {
        [HideInInspector][SerializeField][SingletonReference] private TeleportLocationsManagerAPI teleportLocationsManager;

        public Image sortLocationOrderAscendingImage;
        public Image sortLocationOrderDescendingImage;
        public Image sortLocationNameAscendingImage;
        public Image sortLocationNameDescendingImage;
        public Image sortLocationCategoryAscendingImage;
        public Image sortLocationCategoryDescendingImage;

        /// <summary>
        /// <para><see cref="int"/> locationOrder => <see cref="TeleportLocationRow"/> row</para>
        /// </summary>
        private DataDictionary rowsByOrder = new DataDictionary();
        public TeleportLocationRow[] Rows => (TeleportLocationRow[])rows;
        public int RowsCount => rowsCount;
        public TeleportLocationRow[] UnusedRows => (TeleportLocationRow[])unusedRows;
        public int UnusedRowsCount => unusedRowsCount;

        public override void Initialize()
        {
            base.Initialize();

            currentSortOrderFunction = nameof(CompareRowLocationOrderAscending);
            currentSortOrderImage = sortLocationOrderAscendingImage;
            currentSortOrderImage.enabled = true;
            someRowsAreOutOfSortOrder = false;
        }

        #region RowsManagement

        public bool TryGetRow(int locationOrder, out TeleportLocationRow row)
        {
            if (rowsByOrder.TryGetValue(locationOrder, out DataToken rowToken))
            {
                row = (TeleportLocationRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        public TeleportLocationRow CreateRow(TeleportLocation location)
        {
            TeleportLocationRow row = CreateRowForPlayer(location);
            rowsByOrder.Add(location.Order, row);
            InsertSortNewRow(row);
            return row;
        }

        public void RemoveRow(TeleportLocationRow row)
        {
            rowsByOrder.Remove(row.locationOrder);
            RemoveRow((SortableScrollableRow)row);
        }

        public void RebuildRows() => RebuildRows(teleportLocationsManager.ShownLocationsCount);

        protected override void OnRowCreated(SortableScrollableRow row) { }

        protected override void OnPreRebuildRows()
        {
            rowsByOrder.Clear();
        }

        protected override SortableScrollableRow RebuildRow(int index)
        {
            TeleportLocation location = teleportLocationsManager.ShownLocations[index];
            TeleportLocationRow row = CreateRowForPlayer(location);
            rowsByOrder.Add(location.Order, row);
            return row;
        }

        private TeleportLocationRow CreateRowForPlayer(TeleportLocation location)
        {
            TeleportLocationRow row = (TeleportLocationRow)CreateRow();
            int locationOrder = location.Order;
            row.locationOrder = locationOrder;
            row.location = location;

            string locationName = location.DisplayName.Trim();
            string locationCategory = location.CategoryName.Trim();

            row.sortableOrder = locationOrder;
            row.sortableLocationName = locationName.ToLower();
            row.sortableLocationCategory = locationCategory.ToLower();

            row.locationOrderLabel.text = locationOrder.ToString();
            row.locationNameLabel.text = locationName;
            row.locationCategoryLabel.text = locationCategory;

            return row;
        }

        #endregion

        #region SortHeaders

        // NOTE: Cannot just invert the order of the rows when inverting the order of a sorted column.
        // The categories are the most clear example of this. When inverting the sort order there it makes
        // more sense for just the 2 categories to flip order, while locations within those categories retain
        // relative order.

        public void OnLocationOrderSortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowLocationOrderAscending))
            {
                currentSortOrderFunction = nameof(CompareRowLocationOrderDescending);
                currentSortOrderImage = sortLocationOrderDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowLocationOrderAscending);
                currentSortOrderImage = sortLocationOrderAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        public void OnLocationNameSortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowLocationNameAscending))
            {
                currentSortOrderFunction = nameof(CompareRowLocationNameDescending);
                currentSortOrderImage = sortLocationNameDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowLocationNameAscending);
                currentSortOrderImage = sortLocationNameAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        public void OnLocationCategorySortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowLocationCategoryAscending))
            {
                currentSortOrderFunction = nameof(CompareRowLocationCategoryDescending);
                currentSortOrderImage = sortLocationCategoryDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowLocationCategoryAscending);
                currentSortOrderImage = sortLocationCategoryAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        #endregion

        #region SortAPI

        // public void SortOnPermissionChange(
        //     bool characterNameValue,
        //     bool proximityValue,
        //     bool selectionValue)
        // {
        //     bool isSortedByProximity = currentSortOrderFunction == nameof(CompareRowProximityAscending)
        //         || currentSortOrderFunction == nameof(CompareRowProximityDescending);
        //     if (!characterNameValue
        //         && (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending)
        //             || currentSortOrderFunction == nameof(CompareRowCharacterNameDescending))
        //         || !proximityValue && isSortedByProximity
        //         || !selectionValue
        //         && (currentSortOrderFunction == nameof(CompareRowSelectionAscending)
        //             || currentSortOrderFunction == nameof(CompareRowSelectionDescending)))
        //     {
        //         if (isSortedByProximity)
        //             currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
        //         currentSortOrderImage.enabled = false;
        //         currentSortOrderImage = sortPlayerNameAscendingImage;
        //         currentSortOrderImage.enabled = true;
        //         SortAll();
        //     }
        // }

        // public void PotentiallySortChangedFavoriteRow(TeleportLocationRow row)
        // {
        //     UpdateSortPositionDueToValueChange(row);
        // }

        // public void PotentiallySortChangedPlayerNameRow(TeleportLocationRow row)
        // {
        //     if (currentSortOrderFunction == nameof(CompareRowPlayerNameAscending)
        //         || currentSortOrderFunction == nameof(CompareRowPlayerNameDescending))
        //     {
        //         UpdateSortPositionDueToValueChange(row);
        //     }
        // }

        // public void PotentiallySortChangedCharacterNameRow(TeleportLocationRow row)
        // {
        //     if (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending)
        //         || currentSortOrderFunction == nameof(CompareRowCharacterNameDescending))
        //     {
        //         UpdateSortPositionDueToValueChange(row);
        //     }
        // }

        // public void PotentiallySortChangedProximityRow(TeleportLocationRow row)
        // {
        //     if (currentSortOrderFunction == nameof(CompareRowProximityAscending)
        //         || currentSortOrderFunction == nameof(CompareRowProximityDescending))
        //     {
        //         UpdateSortPositionDueToValueChange(row);
        //     }
        // }

        // public void PotentiallySortChangedSelectionRow(TeleportLocationRow row)
        // {
        //     if (currentSortOrderFunction == nameof(CompareRowSelectionAscending)
        //         || currentSortOrderFunction == nameof(CompareRowSelectionDescending))
        //     {
        //         UpdateSortPositionDueToValueChange(row);
        //     }
        // }

        // public void PotentiallySortChangedSelectionRows()
        // {
        //     if (currentSortOrderFunction == nameof(CompareRowSelectionAscending)
        //         || currentSortOrderFunction == nameof(CompareRowSelectionDescending))
        //     {
        //         UpdateSortPositionsDueToMultipleValueChanges();
        //     }
        // }

        #endregion

        #region MergeSortComparators

        public void CompareRowLocationOrderAscending()
            => leftSortsFirst = ((TeleportLocationRow)compareLeft).sortableOrder <= ((TeleportLocationRow)compareRight).sortableOrder;
        public void CompareRowLocationOrderDescending()
            => leftSortsFirst = ((TeleportLocationRow)compareLeft).sortableOrder >= ((TeleportLocationRow)compareRight).sortableOrder;

        public void CompareRowLocationNameAscending()
            => leftSortsFirst = ((TeleportLocationRow)compareLeft).sortableLocationName
                .CompareTo(((TeleportLocationRow)compareRight).sortableLocationName) <= 0;
        public void CompareRowLocationNameDescending()
            => leftSortsFirst = ((TeleportLocationRow)compareLeft).sortableLocationName
                .CompareTo(((TeleportLocationRow)compareRight).sortableLocationName) >= 0;

        public void CompareRowLocationCategoryAscending()
            => leftSortsFirst = ((TeleportLocationRow)compareLeft).sortableLocationCategory
                .CompareTo(((TeleportLocationRow)compareRight).sortableLocationCategory) <= 0;
        public void CompareRowLocationCategoryDescending()
            => leftSortsFirst = ((TeleportLocationRow)compareLeft).sortableLocationCategory
                .CompareTo(((TeleportLocationRow)compareRight).sortableLocationCategory) >= 0;

        #endregion
    }
}
