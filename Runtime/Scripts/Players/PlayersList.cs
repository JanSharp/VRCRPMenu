using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersList : SortableScrollableList
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;

        private int rpPlayerDataIndex;

        public Image sortPlayerNameAscendingImage;
        public Image sortPlayerNameDescendingImage;
        public Image sortCharacterNameAscendingImage;
        public Image sortCharacterNameDescendingImage;
        public Image sortProximityAscendingImage;
        public Image sortProximityDescendingImage;
        public Image sortSelectionAscendingImage;
        public Image sortSelectionDescendingImage;

        /// <summary>
        /// <para><see cref="uint"/> persistentId => <see cref="PlayersRow"/> row</para>
        /// </summary>
        private DataDictionary rowsByPersistentId = new DataDictionary();
        public PlayersRow[] Rows => (PlayersRow[])rows;
        public int RowsCount => rowsCount;
        public PlayersRow[] UnusedRows => (PlayersRow[])unusedRows;
        public int UnusedRowsCount => unusedRowsCount;

        private VRCPlayerApi localPlayerApi;
        private RPPlayerData localPlayer;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            localPlayerApi = Networking.LocalPlayer;
        }

        public override void Initialize()
        {
            base.Initialize();

            currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
            currentSortOrderImage = sortPlayerNameAscendingImage;
            currentSortOrderImage.enabled = true;
            someRowsAreOutOfSortOrder = false;
        }

        [PlayerDataEvent(PlayerDataEventType.OnLocalPlayerDataAvailable)]
        public void OnLocalPlayerDataAvailable()
        {
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.LocalPlayerData);
        }

        [PlayerDataEvent(PlayerDataEventType.OnAllCustomPlayerDataRegistered)]
        public void OnAllCustomPlayerDataRegistered()
        {
            rpPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<RPPlayerData>(nameof(RPPlayerData));
        }

        #region RowsManagement

        public bool TryGetRow(uint persistentId, out PlayersRow row)
        {
            if (rowsByPersistentId.TryGetValue(persistentId, out DataToken rowToken))
            {
                row = (PlayersRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        public PlayersRow CreateRow(CorePlayerData core)
        {
            PlayersRow row = CreateRowForPlayer(core);
            rowsByPersistentId.Add(row.rpPlayerData.core.persistentId, row);
            InsertSortNewRow(row);
            return row;
        }

        public void RemoveRow(PlayersRow row)
        {
            rowsByPersistentId.Remove(row.rpPlayerData.core.persistentId);
            RemoveRow((SortableScrollableRow)row);
        }

        public void RebuildRows() => RebuildRows(selectionManager.allOnlinePlayersCount);

        protected override void OnRowCreated(SortableScrollableRow row) { }

        protected override void OnPreRebuildRows()
        {
            rowsByPersistentId.Clear();
        }

        protected override SortableScrollableRow RebuildRow(int index)
        {
            CorePlayerData core = selectionManager.allOnlinePlayers[index];
            PlayersRow row = CreateRowForPlayer(core);
            rowsByPersistentId.Add(core.persistentId, row);
            return row;
        }

        private PlayersRow CreateRowForPlayer(CorePlayerData core)
        {
            PlayersRow row = (PlayersRow)CreateRow();
            RPPlayerData rpPlayerData = (RPPlayerData)core.customPlayerData[rpPlayerDataIndex];
            row.rpPlayerData = rpPlayerData;

            bool isFavorite = localPlayer.favoritePlayersOutgoingLut.ContainsKey(core);
            string playerName = rpPlayerData.PlayerDisplayName;
            string characterName = rpPlayerData.characterName;
            bool isSelected = selectionManager.selectedPlayersLut.ContainsKey(core);

            row.isFavorite = isFavorite;
            row.sortablePlayerName = playerName.ToLower();
            row.sortableCharacterName = characterName.ToLower();
            row.sortableProximity = -1f;
            row.sortableSelection = isSelected ? 1 : 0;

            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            row.playerNameLabel.text = playerName;
            row.characterNameLabel.text = characterName;
            row.proximityLabel.text = "-";
            row.selectToggle.SetIsOnWithoutNotify(isSelected);
            row.teleportToButton.interactable = !core.isLocal;
            row.teleportToLabel.interactable = !core.isLocal;

            return row;
        }

        #endregion

        #region Proximity

        private void EvaluateAllProximity()
        {
            Vector3 localPosition = localPlayerApi.GetPosition();
            PlayersRow[] rows = Rows;
            for (int i = 0; i < rowsCount; i++)
            {
                PlayersRow row = rows[i];
                VRCPlayerApi player = row.rpPlayerData.core.playerApi;
                if (player == null)
                {
                    row.sortableProximity = -1f;
                    row.proximityLabel.text = "-";
                    continue;
                }
                float proximity = Vector3.Distance(localPosition, player.GetPosition());
                row.sortableProximity = proximity;
                row.proximityLabel.text = $"{proximity:0.0} m";
            }
        }

        private void ClearProximity()
        {
            PlayersRow[] rows = Rows;
            for (int i = 0; i < rowsCount; i++)
            {
                PlayersRow row = rows[i];
                row.sortableProximity = -1f;
                row.proximityLabel.text = "-";
            }
        }

        #endregion

        #region SortHeaders

        // NOTE: Cannot just invert the order of the rows when inverting the order of a sorted column.
        // The selection is the most clear example of this. When inverting the sort order there it makes more
        // sense for just the 2 groups of players - selected and non selected - to flip order, while players
        // within those "groups" retain relative order

        public void OnPlayerNameSortHeaderClick()
        {
            ClearProximity();
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

        public void OnCharacterNameSortHeaderClick()
        {
            ClearProximity();
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

        public void OnProximitySortHeaderClick()
        {
            EvaluateAllProximity();
            currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowProximityAscending))
            {
                currentSortOrderFunction = nameof(CompareRowProximityDescending);
                currentSortOrderImage = sortProximityDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowProximityAscending);
                currentSortOrderImage = sortProximityAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        public void OnSelectionSortHeaderClick()
        {
            ClearProximity();
            currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowSelectionDescending))
            {
                currentSortOrderFunction = nameof(CompareRowSelectionAscending);
                currentSortOrderImage = sortSelectionAscendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowSelectionDescending);
                currentSortOrderImage = sortSelectionDescendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        #endregion

        #region SortAPI

        public void SortOnPermissionChange(
            bool characterNameValue,
            bool proximityValue,
            bool selectionValue)
        {
            bool isSortedByProximity = currentSortOrderFunction == nameof(CompareRowProximityAscending)
                || currentSortOrderFunction == nameof(CompareRowProximityDescending);
            if (!characterNameValue
                && (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending)
                    || currentSortOrderFunction == nameof(CompareRowCharacterNameDescending))
                || !proximityValue && isSortedByProximity
                || !selectionValue
                && (currentSortOrderFunction == nameof(CompareRowSelectionAscending)
                    || currentSortOrderFunction == nameof(CompareRowSelectionDescending)))
            {
                if (isSortedByProximity)
                    ClearProximity();
                currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
                currentSortOrderImage.enabled = false;
                currentSortOrderImage = sortPlayerNameAscendingImage;
                currentSortOrderImage.enabled = true;
                SortAll();
            }
        }

        public void PotentiallySortChangedFavoriteRow(PlayersRow row)
        {
            UpdateSortPositionDueToValueChange(row);
        }

        public void PotentiallySortChangedPlayerNameRow(PlayersRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowPlayerNameAscending)
                || currentSortOrderFunction == nameof(CompareRowPlayerNameDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        public void PotentiallySortChangedCharacterNameRow(PlayersRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending)
                || currentSortOrderFunction == nameof(CompareRowCharacterNameDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        public void PotentiallySortChangedProximityRow(PlayersRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowProximityAscending)
                || currentSortOrderFunction == nameof(CompareRowProximityDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        public void PotentiallySortChangedSelectionRow(PlayersRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowSelectionAscending)
                || currentSortOrderFunction == nameof(CompareRowSelectionDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        public void PotentiallySortChangedSelectionRows()
        {
            if (currentSortOrderFunction == nameof(CompareRowSelectionAscending)
                || currentSortOrderFunction == nameof(CompareRowSelectionDescending))
            {
                UpdateSortPositionsDueToMultipleValueChanges();
            }
        }

        #endregion

        #region MergeSortComparators

        public void CompareRowPlayerNameAscending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortablePlayerName
                    .CompareTo(right.sortablePlayerName) <= 0;
        }
        public void CompareRowPlayerNameDescending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortablePlayerName
                    .CompareTo(right.sortablePlayerName) >= 0;
        }

        public void CompareRowCharacterNameAscending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
            {
                leftSortsFirst = left.isFavorite;
                return;
            }
            // Empty names always sort last.
            string leftName = left.sortableCharacterName;
            string rightName = right.sortableCharacterName;
            if (leftName == "")
                leftSortsFirst = rightName == "";
            else
                leftSortsFirst = rightName == "" || leftName.CompareTo(rightName) <= 0;
        }
        public void CompareRowCharacterNameDescending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
            {
                leftSortsFirst = left.isFavorite;
                return;
            }
            // Empty names always sort last.
            string leftName = left.sortableCharacterName;
            string rightName = right.sortableCharacterName;
            if (leftName == "")
                leftSortsFirst = rightName == "";
            else
                leftSortsFirst = rightName == "" || leftName.CompareTo(rightName) >= 0;
        }

        public void CompareRowProximityAscending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
            {
                leftSortsFirst = left.isFavorite;
                return;
            }
            // -1 always sorts last.
            float leftProximity = left.sortableProximity;
            float rightProximity = right.sortableProximity;
            if (leftProximity == -1f)
                leftSortsFirst = rightProximity == -1f;
            else
                leftSortsFirst = rightProximity == -1f || leftProximity <= rightProximity;
        }
        public void CompareRowProximityDescending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
            {
                leftSortsFirst = left.isFavorite;
                return;
            }
            // -1 always sorts last.
            float leftProximity = left.sortableProximity;
            float rightProximity = right.sortableProximity;
            if (leftProximity == -1f)
                leftSortsFirst = rightProximity == -1f;
            else
                leftSortsFirst = rightProximity == -1f || leftProximity >= rightProximity;
        }

        public void CompareRowSelectionAscending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableSelection <= right.sortableSelection;
        }
        public void CompareRowSelectionDescending()
        {
            PlayersRow left = (PlayersRow)compareLeft;
            PlayersRow right = (PlayersRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableSelection >= right.sortableSelection;
        }

        #endregion
    }
}
