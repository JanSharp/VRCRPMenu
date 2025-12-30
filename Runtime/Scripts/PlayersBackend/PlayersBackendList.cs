using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendList : SortableScrollableList
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;
        [HideInInspector][SerializeField][FindInParent] private MenuPageRoot menuPageRoot;

        private int rpPlayerDataIndex;
        private int permissionsPlayerDataIndex;

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
        public PlayersBackendRow[] Rows => (PlayersBackendRow[])rows;
        public int RowsCount => rowsCount;

        protected override bool ListIsVisible => menuManager.IsMenuOpen && menuManager.ActivePageInternalName == menuPageRoot.PageInternalName;

        public override void Initialize()
        {
            base.Initialize();

            currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
            currentSortOrderImage = sortPlayerNameAscendingImage;
            currentSortOrderImage.enabled = true;
            someRowsAreOutOfSortOrder = false;

            FetchPlayerDataClassIndexes();
        }

        private void FetchPlayerDataClassIndexes()
        {
            rpPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<RPPlayerData>(nameof(RPPlayerData));
            permissionsPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<PermissionsPlayerData>(nameof(PermissionsPlayerData));
        }

        #region RowsManagement

        public bool TryGetRow(uint persistentId, out PlayersBackendRow row)
        {
            if (rowsByPersistentId.TryGetValue(persistentId, out DataToken rowToken))
            {
                row = (PlayersBackendRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        public PlayersBackendRow CreateRow(CorePlayerData core)
        {
            PlayersBackendRow row = CreateRowForPlayer(core);
            rowsByPersistentId.Add(row.rpPlayerData.core.persistentId, row);
            InsertSortNewRow(row);
            return row;
        }

        public void RemoveRow(PlayersBackendRow row) => RemoveRow((SortableScrollableRow)row);

        public void RebuildRows() => RebuildRows(playerDataManager.AllCorePlayerDataCount);

        protected override void OnRowCreated(SortableScrollableRow row)
        {
            PlayersBackendRow actualRow = (PlayersBackendRow)row;
            actualRow.activeRowHighlightImage.CrossFadeAlpha(0f, 0f, ignoreTimeScale: true);
        }

        protected override void OnPreRebuildRows()
        {
            rowsByPersistentId.Clear();
        }

        protected override SortableScrollableRow RebuildRow(int index)
        {
            CorePlayerData core = playerDataManager.GetCorePlayerDataAt(index);
            PlayersBackendRow row = CreateRowForPlayer(core);
            rowsByPersistentId.Add(core.persistentId, row);
            return row;
        }

        private PlayersBackendRow CreateRowForPlayer(CorePlayerData core)
        {
            PlayersBackendRow row = (PlayersBackendRow)CreateRow();
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

            return row;
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
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowPlayerNameAscending))
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
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameAscending))
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
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowCharacterNameAscending))
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
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowPermissionGroupAscending))
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

        public void SortOnPermissionChange(
            bool displayNameValue,
            bool characterNameValue,
            bool permissionGroupValue)
        {
            if (!displayNameValue
                && (currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameAscending)
                    || currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameDescending))
                || !characterNameValue
                && (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending)
                    || currentSortOrderFunction == nameof(CompareRowCharacterNameDescending))
                || !permissionGroupValue
                && (currentSortOrderFunction == nameof(CompareRowPermissionGroupAscending)
                    || currentSortOrderFunction == nameof(CompareRowPermissionGroupDescending)))
            {
                currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
                currentSortOrderImage.enabled = false;
                currentSortOrderImage = sortPlayerNameAscendingImage;
                currentSortOrderImage.enabled = true;
                SortAll();
            }
        }

        public void PotentiallySortChangedOverriddenDisplayNameRow(PlayersBackendRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameAscending)
                || currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        public void PotentiallySortChangedCharacterNameRow(PlayersBackendRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending)
                || currentSortOrderFunction == nameof(CompareRowCharacterNameDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        public void PotentiallySortChangedPermissionGroupRow(PlayersBackendRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowPermissionGroupAscending)
                || currentSortOrderFunction == nameof(CompareRowPermissionGroupDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        public void PotentiallySortChangedPermissionGroupRows()
        {
            if (currentSortOrderFunction == nameof(CompareRowPermissionGroupAscending)
                || currentSortOrderFunction == nameof(CompareRowPermissionGroupDescending))
            {
                UpdateSortPositionsDueToMultipleValueChanges();
            }
        }

        #endregion

        #region MergeSortComparators

        public void CompareRowPlayerNameAscending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortablePlayerName
                .CompareTo(((PlayersBackendRow)compareRight).sortablePlayerName) <= 0;
        public void CompareRowPlayerNameDescending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortablePlayerName
                .CompareTo(((PlayersBackendRow)compareRight).sortablePlayerName) >= 0;

        public void CompareRowOverriddenDisplayNameAscending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortableOverriddenDisplayName
                .CompareTo(((PlayersBackendRow)compareRight).sortableOverriddenDisplayName) <= 0;
        public void CompareRowOverriddenDisplayNameDescending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortableOverriddenDisplayName
                .CompareTo(((PlayersBackendRow)compareRight).sortableOverriddenDisplayName) >= 0;

        public void CompareRowCharacterNameAscending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortableCharacterName
                .CompareTo(((PlayersBackendRow)compareRight).sortableCharacterName) <= 0;
        public void CompareRowCharacterNameDescending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortableCharacterName
                .CompareTo(((PlayersBackendRow)compareRight).sortableCharacterName) >= 0;

        public void CompareRowPermissionGroupAscending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortablePermissionGroupName
                .CompareTo(((PlayersBackendRow)compareRight).sortablePermissionGroupName) <= 0;
        public void CompareRowPermissionGroupDescending()
            => leftSortsFirst = ((PlayersBackendRow)compareLeft).sortablePermissionGroupName
                .CompareTo(((PlayersBackendRow)compareRight).sortablePermissionGroupName) >= 0;

        #endregion
    }
}
