using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendPage : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private int rpPlayerDataIndex;
        private int permissionsPlayerDataIndex;

        public GameObject rowPrefab;
        public Transform rowsParent;
        public Image sortPlayerNameAscendingImage;
        public Image sortPlayerNameDescendingImage;
        public Image sortOverriddenDisplayNameAscendingImage;
        public Image sortOverriddenDisplayNameDescendingImage;
        public Image sortCharacterNameAscendingImage;
        public Image sortCharacterNameDescendingImage;
        public Image sortPermissionGroupAscendingImage;
        public Image sortPermissionGroupDescendingImage;

        /// <summary>
        /// <para><see cref="uint"/> persistentId => <see cref="PlayersBackendRow"/> row</para>
        /// </summary>
        private DataDictionary rowsByPersistentId = new DataDictionary();
        private PlayersBackendRow[] rows = new PlayersBackendRow[ArrList.MinCapacity];
        private int rowsCount = 0;
        private PlayersBackendRow[] unusedRows = new PlayersBackendRow[ArrList.MinCapacity];
        private int unusedRowsCount = 0;
        private string currentSortOrderFunction;
        private Image currentSortOrderImage;

        private void Start()
        {
            currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
            currentSortOrderImage = sortPlayerNameAscendingImage;
            currentSortOrderImage.enabled = true;
        }

        private void FetchPlayerDataClassIndexes()
        {
            rpPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<RPPlayerData>(nameof(RPPlayerData));
            permissionsPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<PermissionsPlayerData>(nameof(PermissionsPlayerData));
        }

        [PlayerDataEvent(PlayerDataEventType.OnPrePlayerDataManagerInit)]
        public void OnPrePlayerDataManagerInit()
        {
            FetchPlayerDataClassIndexes();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            FetchPlayerDataClassIndexes();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            PlayersBackendRow row = CreateRowForPlayer(playerDataManager.PlayerDataForEvent);
            InsertSortNewRow(row);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            CorePlayerData core = playerDataManager.PlayerDataForEvent;
            if (!rowsByPersistentId.Remove(core.persistentId, out DataToken rowToken))
            {
                // Somebody could delete player data inside of OnPlayerDataImportFinished, but before
                // our handler has ran, thus deleting player data which we are not yet aware of.
                // The rows are about to be rebuilt in that case, so just ignore.
                // Or somebody could create offline player data and somebody decides to delete it in the
                // created event before we receive the created event.
                // (Once the API to create offline player data exists.)
                return;
            }
            PlayersBackendRow row = (PlayersBackendRow)rowToken.Reference;
            row.gameObject.SetActive(false);
            row.transform.SetAsLastSibling();
            ArrList.Add(ref unusedRows, ref unusedRowsCount, row);
            int index = row.index;
            ArrList.RemoveAt(ref rows, ref rowsCount, index);
            for (int i = index; i < rowsCount; i++)
                rows[i].SetIndex(i);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataImportFinished)]
        public void OnPlayerDataImportFinished()
        {
            int newCount = playerDataManager.AllCorePlayerDataCount;

            rowsByPersistentId.Clear();
            ArrList.AddRange(ref unusedRows, ref unusedRowsCount, rows, rowsCount);
            if (newCount < rowsCount)
                for (int i = 0; i < rowsCount - newCount; i++)
                {
                    // Disable the low index ones, the higher ones will be reused from the unusedRows "stack".
                    PlayersBackendRow row = rows[i];
                    row.gameObject.SetActive(false);
                    row.transform.SetAsLastSibling();
                }

            CorePlayerData[] allCorePlayerData = playerDataManager.AllCorePlayerDataRaw;
            int count = playerDataManager.AllCorePlayerDataCount;
            for (int i = 0; i < count; i++)
            {
                CorePlayerData core = allCorePlayerData[i];
                PlayersBackendRow row = CreateRowForPlayer(core);
                rows[i] = row;
                rowsByPersistentId.Add(core.persistentId, row);
            }
            rowsCount = newCount;

            MergeSortAndReorder();
        }

        private PlayersBackendRow CreateRowForPlayer(CorePlayerData core)
        {
            PlayersBackendRow row = CreateRow();
            RPPlayerData rpPlayerData = (RPPlayerData)core.customPlayerData[rpPlayerDataIndex];
            PermissionsPlayerData permissionsPlayerData = (PermissionsPlayerData)core.customPlayerData[permissionsPlayerDataIndex];
            row.rpPlayerData = rpPlayerData;
            row.permissionsPlayerData = permissionsPlayerData;

            string playerName = core.displayName;
            string characterName = rpPlayerData.characterName;
            string groupName = permissionsPlayerData.permissionGroup.groupName;

            row.sortablePlayerName = playerName.ToLower();
            row.sortableOverriddenDisplayName = rpPlayerData.PlayerDisplayName.ToLower();
            row.sortableCharacterName = characterName.ToLower();
            row.sortablePermissionGroupName = groupName.ToLower();

            row.playerNameLabel.text = playerName;
            row.overriddenDisplayNameField.text = rpPlayerData.overriddenDisplayName ?? "";
            row.overriddenDisplayNameLabel.text = playerName;
            row.characterNameField.text = characterName;
            row.permissionGroupLabel.text = groupName;

            row.gameObject.SetActive(true);
            return row;
        }

        private PlayersBackendRow CreateRow()
        {
            if (unusedRowsCount != 0)
                return ArrList.RemoveAt(ref unusedRows, ref unusedRowsCount, unusedRowsCount - 1);
            GameObject go = Instantiate(rowPrefab);
            go.transform.SetParent(rowsParent, worldPositionStays: false);
            return go.GetComponent<PlayersBackendRow>();
        }

        private void InsertSortNewRow(PlayersBackendRow row)
        {
            rowsByPersistentId.Add(row, row.rpPlayerData.core.persistentId);
            if (rowsCount == 0)
            {
                ArrList.Add(ref rows, ref rowsCount, row);
                row.SetIndex(0);
                return;
            }
            compareRight = row;
            int index = rowsCount;
            do
            {
                compareLeft = rows[--index];
                SendCustomEvent(currentSortOrderFunction);
            }
            while (!leftSortsFirst);
            index++; // The above loop overshoots by 1.
            row.transform.SetSiblingIndex(index + 1); // +1 because the prefab resides at index 0.
            ArrList.Insert(ref rows, ref rowsCount, row, index);
            for (int i = index; i < rowsCount; i++)
                rows[i].SetIndex(i);
        }

        public void OnOverriddenDisplayNameChanged(PlayersBackendRow row)
        {
            // TODO: Send input action.
        }

        public void OnCharacterNameChanged(PlayersBackendRow row)
        {
            // TODO: Send input action.
        }

        public void OnPermissionGroupClick(PlayersBackendRow row)
        {
            // TODO: Show permission group dropdown popup.
        }

        public void OnDeleteClick(PlayersBackendRow row)
        {
            // TODO: Show confirmation popup.
        }

        #region SortHeaders

        // NOTE: Cannot just invert the order of the rows when inverting the order of a sorted column.
        // The permission groups are the most clear example of this. When inverting the sort order there it
        // makes more sense for just the groups to flip order, while players in that group to retain relative
        // order

        public void OnPlayerNameSortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (currentSortOrderFunction == nameof(CompareRowPlayerNameAscending))
            {
                currentSortOrderFunction = nameof(CompareRowPlayerNameDescending);
                currentSortOrderImage = sortPlayerNameDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
                currentSortOrderImage = sortPlayerNameAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            MergeSortAndReorder();
        }

        public void OnOverriddenDisplayNameSortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameAscending))
            {
                currentSortOrderFunction = nameof(CompareRowOverriddenDisplayNameDescending);
                currentSortOrderImage = sortOverriddenDisplayNameDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowOverriddenDisplayNameAscending);
                currentSortOrderImage = sortOverriddenDisplayNameAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            MergeSortAndReorder();
        }

        public void OnCharacterNameSortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending))
            {
                currentSortOrderFunction = nameof(CompareRowCharacterNameDescending);
                currentSortOrderImage = sortCharacterNameDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowCharacterNameAscending);
                currentSortOrderImage = sortCharacterNameAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            MergeSortAndReorder();
        }

        public void OnPermissionGroupSortHeaderClick()
        {
            currentSortOrderImage.enabled = false;
            if (currentSortOrderFunction == nameof(CompareRowPermissionGroupAscending))
            {
                currentSortOrderFunction = nameof(CompareRowPermissionGroupDescending);
                currentSortOrderImage = sortPermissionGroupDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowPermissionGroupAscending);
                currentSortOrderImage = sortPermissionGroupAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            MergeSortAndReorder();
        }

        private void MergeSortAndReorder()
        {
            MergeSort(currentSortOrderFunction);
            for (int i = 0; i < rowsCount; i++)
            {
                var row = rows[i];
                row.SetIndex(i);
                row.transform.SetSiblingIndex(i + 1); // +1 because the prefab resides at index 0.
            }
        }

        #endregion

        #region MergeSortComparators

        private PlayersBackendRow compareLeft;
        private PlayersBackendRow compareRight;
        private bool leftSortsFirst;

        public void CompareRowPlayerNameAscending()
            => leftSortsFirst = compareLeft.sortablePlayerName.CompareTo(compareRight.sortablePlayerName) <= 0;
        public void CompareRowPlayerNameDescending()
            => leftSortsFirst = compareLeft.sortablePlayerName.CompareTo(compareRight.sortablePlayerName) >= 0;

        public void CompareRowOverriddenDisplayNameAscending()
            => leftSortsFirst = compareLeft.sortableOverriddenDisplayName.CompareTo(compareRight.sortableOverriddenDisplayName) <= 0;
        public void CompareRowOverriddenDisplayNameDescending()
            => leftSortsFirst = compareLeft.sortableOverriddenDisplayName.CompareTo(compareRight.sortableOverriddenDisplayName) >= 0;

        public void CompareRowCharacterNameAscending()
            => leftSortsFirst = compareLeft.sortableCharacterName.CompareTo(compareRight.sortableCharacterName) <= 0;
        public void CompareRowCharacterNameDescending()
            => leftSortsFirst = compareLeft.sortableCharacterName.CompareTo(compareRight.sortableCharacterName) >= 0;

        public void CompareRowPermissionGroupAscending()
            => leftSortsFirst = compareLeft.sortablePermissionGroupName.CompareTo(compareRight.sortablePermissionGroupName) <= 0;
        public void CompareRowPermissionGroupDescending()
            => leftSortsFirst = compareLeft.sortablePermissionGroupName.CompareTo(compareRight.sortablePermissionGroupName) >= 0;

        #endregion

        #region MergeSort

        /// <summary>
        /// <para>Merge sort is a stable sorting algorithm.</para>
        /// </summary>
        /// <param name="sortFunctionName"></param>
        private void MergeSort(string sortFunctionName)
        {
            if (rowsCount <= 1)
                return;
            ArrList.EnsureCapacity(ref mergeSortRowsCopy, rowsCount);
            mergeSortStack[0] = 0;
            mergeSortStack[1] = rowsCount;
            mergeSortStackTop = 1;
            mergeSortSortFunction = sortFunctionName;
            MergeSortRecursive();
        }

        private PlayersBackendRow[] mergeSortRowsCopy = new PlayersBackendRow[ArrList.MinCapacity];
        private int[] mergeSortStack = new int[32];
        private int mergeSortStackTop = -1;
        private string mergeSortSortFunction;

        /// <summary>
        /// <para>Not using <see cref="RecursiveMethodAttribute"/>, manually "optimized".</para>
        /// </summary>
        private void MergeSortRecursive()
        {
            int count = mergeSortStack[mergeSortStackTop];
            if (count <= 1)
            {
                mergeSortStackTop -= 2; // Pop args for this MergeSortRecursive call.
                return;
            }
            int index = mergeSortStack[mergeSortStackTop - 1];
            int leftCount = count / 2;

            ArrList.EnsureCapacity(ref mergeSortStack, mergeSortStackTop + 5); // 5 instead of 4, because top == count - 1.
            mergeSortStack[++mergeSortStackTop] = index + leftCount; // Push args for the second MergeSortRecursive call.
            mergeSortStack[++mergeSortStackTop] = count - leftCount;

            if (leftCount > 1) // Duplicated early check for optimization.
            {
                mergeSortStack[++mergeSortStackTop] = index;
                mergeSortStack[++mergeSortStackTop] = leftCount;
                MergeSortRecursive();
            }

            MergeSortRecursive();

            Merge(mergeSortStack[mergeSortStackTop - 1], mergeSortStack[mergeSortStackTop]);
            mergeSortStackTop -= 2; // Pop args for this MergeSortRecursive call.
        }

        private void Merge(int startIndex, int count)
        {
            int leftCount = count / 2;
            int rightCount = count - leftCount;
            System.Array.Copy(rows, startIndex, mergeSortRowsCopy, 0, leftCount + rightCount);

            int leftIndex = 0;
            int rightIndex = 0;
            int targetIndex = startIndex;
            // Compare until reaching the end of either left or right.
            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                compareLeft = mergeSortRowsCopy[leftIndex];
                compareRight = mergeSortRowsCopy[leftCount + rightIndex];
                SendCustomEvent(mergeSortSortFunction);
                if (leftSortsFirst)
                    rows[targetIndex++] = mergeSortRowsCopy[leftIndex++];
                else
                    rows[targetIndex++] = mergeSortRowsCopy[leftCount + rightIndex++];
            }

            if (leftIndex < leftCount) // Copy remaining left.
                System.Array.Copy(mergeSortRowsCopy, leftIndex, rows, targetIndex, leftCount - leftIndex);
            else // Otherwise copy remaining right (guaranteed to be at least 1 remaining in right).
                System.Array.Copy(mergeSortRowsCopy, leftCount + rightIndex, rows, targetIndex, rightCount - rightIndex);
        }

        #endregion
    }
}
