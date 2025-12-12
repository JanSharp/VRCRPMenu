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
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;
        [HideInInspector][SerializeField][SingletonReference] private MenuManager menuManager;

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
        public Transform popupsParent;
        public RectTransform confirmDeletePopup;

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

        private CorePlayerData playerDataAwaitingDeleteConfirmation;

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
            RebuildRows();
        }

        #region RowsManagement

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            var row = CreateRowForPlayer(playerDataManager.PlayerDataForEvent);
            rowsByPersistentId.Add(row, row.rpPlayerData.core.persistentId);
            InsertSortNewRow(row);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline()
        {
            CorePlayerData core = playerDataManager.PlayerDataForEvent;
            OnPlayerDataOfflineStateChanged(core);
            if (core == playerDataAwaitingDeleteConfirmation)
                menuManager.ClosePopup(confirmDeletePopup, doCallback: true);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline()
        {
            OnPlayerDataOfflineStateChanged(playerDataManager.PlayerDataForEvent);
        }

        private void OnPlayerDataOfflineStateChanged(CorePlayerData core)
        {
            if (!rowsByPersistentId.TryGetValue(core.persistentId, out DataToken rowToken))
                return; // Some system did something weird.
            PlayersBackendRow row = (PlayersBackendRow)rowToken.Reference;
            row.deleteButton.interactable = core.isOffline;
            row.deleteLabel.interactable = core.isOffline;
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            CorePlayerData core = playerDataManager.PlayerDataForEvent;
            if (core == playerDataAwaitingDeleteConfirmation)
                menuManager.ClosePopup(confirmDeletePopup, doCallback: true);
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
            RebuildRows();
        }

        private void RebuildRows()
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

            SortAll();
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
            row.overriddenDisplayNameField.SetTextWithoutNotify(rpPlayerData.overriddenDisplayName ?? "");
            row.overriddenDisplayNameLabel.text = playerName;
            row.characterNameField.SetTextWithoutNotify(characterName);
            row.permissionGroupLabel.text = groupName;
            row.deleteButton.interactable = core.isOffline;
            row.deleteLabel.interactable = core.isOffline;

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

        #endregion

        #region OverriddenDisplayName

        public void OnOverriddenDisplayNameChanged(PlayersBackendRow row)
        {
            string inputText = row.overriddenDisplayNameField.text.Trim();
            if (inputText == "")
                inputText = null;
            playersBackendManager.SendSetOverriddenDisplayNameIA(row.rpPlayerData, inputText);
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged)]
        public void OnRPPlayerDataOverriddenDisplayNameChanged()
        {
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!rowsByPersistentId.TryGetValue(rpPlayerData.core.persistentId, out DataToken rowToken))
                return; // Some system did something weird.
            PlayersBackendRow row = (PlayersBackendRow)rowToken.Reference;
            row.sortableOverriddenDisplayName = rpPlayerData.PlayerDisplayName.ToLower();
            row.overriddenDisplayNameField.SetTextWithoutNotify(rpPlayerData.overriddenDisplayName);

            if (currentSortOrderFunction != nameof(CompareRowOverriddenDisplayNameAscending)
                && currentSortOrderFunction != nameof(CompareRowOverriddenDisplayNameDescending))
            {
                return;
            }
            // TODO: Only sort if the page is not visible.
            // TODO: If the page is visible and by not moving the row it ends up no longer being sorted, disable
            // the sort order icon and set a flag to make it always pick default sort order when clicking a header.
            SortOne(row);
        }

        #endregion

        #region CharacterName

        public void OnCharacterNameChanged(PlayersBackendRow row)
        {
            string inputText = row.characterNameField.text.Trim();
            playersBackendManager.SendSetCharacterNameIA(row.rpPlayerData, inputText);
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged)]
        public void OnRPPlayerDataCharacterNameChanged()
        {
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!rowsByPersistentId.TryGetValue(rpPlayerData.core.persistentId, out DataToken rowToken))
                return; // Some system did something weird.
            PlayersBackendRow row = (PlayersBackendRow)rowToken.Reference;
            string characterName = rpPlayerData.characterName;
            row.sortableCharacterName = characterName.ToLower();
            row.characterNameField.SetTextWithoutNotify(characterName);

            if (currentSortOrderFunction != nameof(CompareRowCharacterNameAscending)
                && currentSortOrderFunction != nameof(CompareRowCharacterNameDescending))
            {
                return;
            }
            // TODO: Only sort if the page is not visible.
            // TODO: If the page is visible and by not moving the row it ends up no longer being sorted, disable
            // the sort order icon and set a flag to make it always pick default sort order when clicking a header.
            SortOne(row);
        }

        #endregion

        #region PermissionGroups

        public void OnPermissionGroupClick(PlayersBackendRow row)
        {
            // TODO: Show permission group dropdown popup.
        }

        [PermissionsEvent(PermissionsEventType.OnPlayerPermissionGroupChanged)]
        public void OnPlayerPermissionGroupChanged()
        {
            PermissionsPlayerData permissionsPlayerData = permissionManager.PlayerDataForEvent;
            if (!rowsByPersistentId.TryGetValue(permissionsPlayerData.core.persistentId, out DataToken rowToken))
                return; // Some system did something weird.
            PlayersBackendRow row = (PlayersBackendRow)rowToken.Reference;
            string permissionGroupName = permissionsPlayerData.permissionGroup.groupName;
            row.sortablePermissionGroupName = permissionGroupName.ToLower();
            row.permissionGroupLabel.text = permissionGroupName;

            if (currentSortOrderFunction != nameof(CompareRowPermissionGroupAscending)
                && currentSortOrderFunction != nameof(CompareRowPermissionGroupDescending))
            {
                return;
            }
            // TODO: Only sort if the page is not visible.
            // TODO: If the page is visible and by not moving the row it ends up no longer being sorted, disable
            // the sort order icon and set a flag to make it always pick default sort order when clicking a header.
            SortOne(row);
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupRenamed)]
        public void OnPermissionGroupRenamed()
        {
            PermissionGroup renamedGroup = permissionManager.RenamedPermissionGroup;
            string permissionGroupName = renamedGroup.groupName;
            string sortablePermissionGroupName = permissionGroupName.ToLower();

            int affectedCount = 0;
            for (int i = 0; i < rowsCount; i++)
            {
                PlayersBackendRow row = rows[i];
                if (row.permissionsPlayerData.permissionGroup != renamedGroup)
                    continue;
                affectedCount++;
                row.sortablePermissionGroupName = sortablePermissionGroupName;
                row.permissionGroupLabel.text = permissionGroupName;
            }

            if (affectedCount == 0)
                return;
            if (currentSortOrderFunction != nameof(CompareRowPermissionGroupAscending)
                && currentSortOrderFunction != nameof(CompareRowPermissionGroupDescending))
            {
                return;
            }
            // TODO: Only sort if the page is not visible, when not sorting just unconditionally disable the sort icon.
            SortAll();
        }

        #endregion

        #region Delete

        public void OnDeleteClick(PlayersBackendRow row)
        {
            playerDataAwaitingDeleteConfirmation = row.rpPlayerData.core;
            confirmDeletePopup.SetParent(row.confirmDeletePopupLocation, worldPositionStays: false);
            confirmDeletePopup.anchoredPosition = Vector2.zero;
            menuManager.ShowPopupAtCurrentPosition(
                confirmDeletePopup,
                this,
                nameof(OnConfirmDeletePopupClosed),
                minDistanceFromPageEdge: 0f);
        }

        public void OnConfirmDeletePopupClosed()
        {
            confirmDeletePopup.SetParent(popupsParent, worldPositionStays: false);
            confirmDeletePopup.anchoredPosition = Vector2.zero;
            playerDataAwaitingDeleteConfirmation = null;
        }

        public void OnConfirmDeleteClick()
        {
            CorePlayerData toDelete = playerDataAwaitingDeleteConfirmation;
            menuManager.ClosePopup(confirmDeletePopup, doCallback: true); // Clears rowAwaitingDeleteConfirmation.
            if (toDelete == null || toDelete.isDeleted) // Shouldn't really be possible but just to be sure.
                return;
            playerDataManager.SendDeleteOfflinePlayerDataIA(toDelete);
        }

        #endregion

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
            SortAll();
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
            SortAll();
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
            SortAll();
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
            SortAll();
        }

        #endregion

        #region SortAPI

        /// <summary>
        /// <para>Adds <paramref name="row"/> to <see cref="rows"/>.</para>
        /// </summary>
        /// <param name="row"></param>
        private void InsertSortNewRow(PlayersBackendRow row)
        {
            if (rowsCount == 0)
            {
                row.transform.SetSiblingIndex(1); // The prefab resides at 0.
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

        /// <summary>
        /// <para><paramref name="row"/> must already be in <see cref="rows"/>.</para>
        /// </summary>
        /// <param name="row"></param>
        private void SortOne(PlayersBackendRow row)
        {
            int index = row.index;
            int initialIndex = index;

            while (index > 0) // Try move left.
            {
                compareLeft = rows[index - 1];
                compareRight = rows[index];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                rows[index] = compareLeft;
                compareLeft.SetIndex(index);
                index--;
            }
            if (index != initialIndex)
            {
                row.transform.SetSiblingIndex(index + 1); // +1 because the prefab resides at index 0.
                rows[index] = row;
                row.SetIndex(index);
                return;
            }

            while (index < rowsCount - 1) // Try move right.
            {
                compareLeft = rows[index];
                compareRight = rows[index + 1];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                rows[index] = compareRight;
                compareRight.SetIndex(index);
                index++;
            }
            if (index != initialIndex)
            {
                row.transform.SetSiblingIndex(index + 1); // +1 because the prefab resides at index 0.
                rows[index] = row;
                row.SetIndex(index);
                return;
            }
        }

        private void SortAll()
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
