using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendPage : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;
        [HideInInspector][SerializeField][FindInParent] private MenuPageRoot menuPageRoot;

        private int rpPlayerDataIndex;
        private int permissionsPlayerDataIndex;

        public GameObject rowPrefab;
        public PlayersBackendRow rowPrefabScript;
        public Transform rowsParent;
        public RectTransform rowsViewport;
        public RectTransform rowsContent;
        private float rowHeight;
        private float negativeRowHeight;
        private float currentRowsContentHeight;
        private int prevFirstVisibleRowIndex = 0;
        private int prevFirstInvisibleRowIndex = 0;
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
        public RectTransform permissionGroupPopup;
        public GameObject permissionGroupPrefab;
        public Transform permissionGroupsParent;
        [Min(0)]
        public int permissionGroupButtonSiblingIndexBaseOffset;
        public ScrollRect permissionGroupsScrollRect;
        private float permissionGroupButtonHeight;
        private float maxPermissionGroupsPopupHeight;

        [PermissionDefinitionReference(nameof(editDisplayNamePermissionDef))]
        public string editDisplayNamePermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editDisplayNamePermissionDef;

        [PermissionDefinitionReference(nameof(editCharacterNamePermissionDef))]
        public string editCharacterNamePermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editCharacterNamePermissionDef;

        [PermissionDefinitionReference(nameof(editPermissionGroupPermissionDef))]
        public string editPermissionGroupPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editPermissionGroupPermissionDef;

        [PermissionDefinitionReference(nameof(deleteOfflinePlayerDataPermissionDef))]
        public string deleteOfflinePlayerDataPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition deleteOfflinePlayerDataPermissionDef;

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
        private bool someRowsAreOutOfSortOrder;

        private CorePlayerData playerDataAwaitingDeleteConfirmation;

        /// <summary>
        /// <para><see cref="uint"/> permissionGroupId => <see cref="PlayersBackendPermissionGroupButton"/> button</para>
        /// </summary>
        private DataDictionary pgButtonsById = new DataDictionary();
        private PlayersBackendPermissionGroupButton[] pgButtons = new PlayersBackendPermissionGroupButton[ArrList.MinCapacity];
        private int pgButtonsCount = 0;
        private PlayersBackendPermissionGroupButton[] unusedPGButtons = new PlayersBackendPermissionGroupButton[ArrList.MinCapacity];
        private int unusedPGButtonsCount = 0;

        private PlayersBackendRow selectedRowForPermissionGroupEditing;
        private PlayersBackendPermissionGroupButton selectedPermissionGroupButton;

        private uint localPlayerId;
        private bool isInitialized = false;

        public bool PageIsVisible => menuManager.IsMenuOpen && menuManager.ActivePageInternalName == menuPageRoot.PageInternalName;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
            currentSortOrderFunction = nameof(CompareRowPlayerNameAscending);
            currentSortOrderImage = sortPlayerNameAscendingImage;
            currentSortOrderImage.enabled = true;
            someRowsAreOutOfSortOrder = false;

            rowHeight = rowPrefabScript.rowRect.sizeDelta.y;
            negativeRowHeight = -rowHeight;
            permissionGroupButtonHeight = permissionGroupPrefab.GetComponent<LayoutElement>().preferredHeight;
            maxPermissionGroupsPopupHeight = permissionGroupPopup.sizeDelta.y;
        }

        private void Update()
        {
            ShowOnlyRowsVisibleInViewport();
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
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            RebuildPermissionGroupButtons();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            FetchPlayerDataClassIndexes();
            RebuildRows();
            RebuildPermissionGroupButtons();
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            EnsureClosedPopups();
        }

        private void EnsureClosedPopups()
        {
            EnsureClosedConfirmDeletePopup();
            EnsureClosedPermissionGroupPopup();
        }

        private void EnsureClosedConfirmDeletePopup()
        {
            if (playerDataAwaitingDeleteConfirmation != null)
                menuManager.ClosePopup(confirmDeletePopup, doCallback: true);
        }

        private void EnsureClosedPermissionGroupPopup()
        {
            if (selectedRowForPermissionGroupEditing != null)
                menuManager.ClosePopup(permissionGroupPopup, doCallback: true);
        }

        #region PermissionResolution

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            bool displayNameValue = editDisplayNamePermissionDef.valueForLocalPlayer;
            bool characterNameValue = editCharacterNamePermissionDef.valueForLocalPlayer;
            bool permissionGroupValue = editPermissionGroupPermissionDef.valueForLocalPlayer;
            bool deleteValue = deleteOfflinePlayerDataPermissionDef.valueForLocalPlayer;

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

            if (!permissionGroupValue)
                EnsureClosedPermissionGroupPopup();
            if (!deleteValue)
                EnsureClosedConfirmDeletePopup();

            bool displayNameChanged = rowPrefabScript.overriddenDisplayNameRoot.activeSelf != displayNameValue;
            bool characterNameChanged = rowPrefabScript.characterNameRoot.activeSelf != characterNameValue;
            bool permissionGroupChanged = rowPrefabScript.permissionGroupRoot.activeSelf != permissionGroupValue;
            bool deleteChanged = rowPrefabScript.deleteRoot.activeSelf != deleteValue;
            rowPrefabScript.overriddenDisplayNameRoot.SetActive(displayNameValue);
            rowPrefabScript.characterNameRoot.SetActive(characterNameValue);
            rowPrefabScript.permissionGroupRoot.SetActive(permissionGroupValue);
            rowPrefabScript.deleteRoot.SetActive(deleteValue);

            for (int i = 0; i < rowsCount; i++)
            {
                // I'm thinking that in any case where 2 permissions changed it is faster to do all 4 ifs
                // every loop, because I have heard that Udon arrays are slow. But it's just a guess.
                PlayersBackendRow row = rows[i];
                if (displayNameChanged)
                    row.overriddenDisplayNameRoot.SetActive(displayNameValue);
                if (characterNameChanged)
                    row.characterNameRoot.SetActive(characterNameValue);
                if (permissionGroupChanged)
                    row.permissionGroupRoot.SetActive(permissionGroupValue);
                if (deleteChanged)
                    row.deleteRoot.SetActive(deleteValue);
            }
        }

        #endregion

        #region RowsManagement

        private bool TryGetRow(uint persistentId, out PlayersBackendRow row)
        {
            if (rowsByPersistentId.TryGetValue(persistentId, out DataToken rowToken))
            {
                row = (PlayersBackendRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            if (!isInitialized)
                return;
            PlayersBackendRow row = CreateRowForPlayer(playerDataManager.PlayerDataForEvent);
            rowsByPersistentId.Add(row.rpPlayerData.core.persistentId, row);
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
            if (!TryGetRow(core.persistentId, out PlayersBackendRow row))
                return; // Some system did something weird.
            row.deleteButton.interactable = core.isOffline;
            row.deleteLabel.interactable = core.isOffline;
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            if (!isInitialized)
                return;
            CorePlayerData core = playerDataManager.PlayerDataForEvent;
            if (core == playerDataAwaitingDeleteConfirmation)
                menuManager.ClosePopup(confirmDeletePopup, doCallback: true);
            if (!TryGetRow(core.persistentId, out PlayersBackendRow row))
            {
                // Somebody could delete player data inside of OnPlayerDataImportFinished, but before
                // our handler has ran, thus deleting player data which we are not yet aware of.
                // The rows are about to be rebuilt in that case, so just ignore.
                // Or somebody could create offline player data and somebody decides to delete it in the
                // created event before we receive the created event.
                // (Once the API to create offline player data exists.)
                return;
            }
            row.rowGo.SetActive(false);
            ArrList.Add(ref unusedRows, ref unusedRowsCount, row);
            int index = row.index;
            ArrList.RemoveAt(ref rows, ref rowsCount, index);
            for (int i = index; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataImportFinished)]
        public void OnPlayerDataImportFinished()
        {
            RebuildRows();
        }

        private void RebuildRows()
        {
            EnsureClosedPopups();

            HideAllCurrentlyVisibleRows();
            rowsByPersistentId.Clear();
            ArrList.AddRange(ref unusedRows, ref unusedRowsCount, rows, rowsCount);

            rowsCount = playerDataManager.AllCorePlayerDataCount;
            ArrList.EnsureCapacity(ref rows, rowsCount);
            currentRowsContentHeight = rowsCount * rowHeight;
            rowsContent.sizeDelta = new Vector2(0f, currentRowsContentHeight);

            CorePlayerData[] allCorePlayerData = playerDataManager.AllCorePlayerDataRaw;
            for (int i = 0; i < rowsCount; i++)
            {
                CorePlayerData core = allCorePlayerData[i];
                PlayersBackendRow row = CreateRowForPlayer(core);
                rows[i] = row;
                rowsByPersistentId.Add(core.persistentId, row);
            }

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

            return row;
        }

        private PlayersBackendRow CreateRow()
        {
            if (unusedRowsCount != 0)
                return ArrList.RemoveAt(ref unusedRows, ref unusedRowsCount, unusedRowsCount - 1);
            GameObject go = Instantiate(rowPrefab);
            go.transform.SetParent(rowsParent, worldPositionStays: false);
            PlayersBackendRow row = go.GetComponent<PlayersBackendRow>();
            row.activeRowHighlightImage.CrossFadeAlpha(0f, 0f, ignoreTimeScale: true);
            return row;
        }

        private void HideAllCurrentlyVisibleRows()
        {
            for (int i = prevFirstVisibleRowIndex; i < prevFirstInvisibleRowIndex; i++)
                rows[i].rowGo.SetActive(false);
            prevFirstVisibleRowIndex = 0;
            prevFirstInvisibleRowIndex = 0;
        }

        private void ShowOnlyRowsVisibleInViewport()
        {
            // Always recalculate in order to support the size changing at runtime. Even though at the time of
            // writing this with the current page setup it is static, things might change, people might use it
            // differently.
            float viewportHeight = rowsViewport.rect.height;
            int visibleCount = Mathf.CeilToInt(viewportHeight / rowHeight) + 1;
            float position = rowsContent.anchoredPosition.y;
            int firstVisibleIndex = Mathf.FloorToInt(position / rowHeight);
            int firstInvisibleIndex = firstVisibleIndex + visibleCount;

            if (firstVisibleIndex < 0)
                firstVisibleIndex = 0;
            if (firstInvisibleIndex > rowsCount)
                firstInvisibleIndex = rowsCount;

            if (firstInvisibleIndex <= prevFirstVisibleRowIndex // New range is entirely below, no overlap.
                    || firstVisibleIndex >= prevFirstInvisibleRowIndex) // New range is entirely above, no overlap.
            {
                for (int i = prevFirstVisibleRowIndex; i < prevFirstInvisibleRowIndex; i++)
                    rows[i].rowGo.SetActive(false);
                for (int i = firstVisibleIndex; i < firstInvisibleIndex; i++)
                    rows[i].rowGo.SetActive(true);
                prevFirstVisibleRowIndex = firstVisibleIndex;
                prevFirstInvisibleRowIndex = firstInvisibleIndex;
                return;
            }
            // There is overlap.
            // The logic below handles prev and new ranges being of different sizes (visible counts).

            if (prevFirstVisibleRowIndex < firstVisibleIndex)
                for (int i = prevFirstVisibleRowIndex; i < firstVisibleIndex; i++)
                    rows[i].rowGo.SetActive(false);
            else
                for (int i = firstVisibleIndex; i < prevFirstVisibleRowIndex; i++)
                    rows[i].rowGo.SetActive(true);

            if (prevFirstInvisibleRowIndex < firstInvisibleIndex)
                for (int i = prevFirstInvisibleRowIndex; i < firstInvisibleIndex; i++)
                    rows[i].rowGo.SetActive(true);
            else
                for (int i = firstInvisibleIndex; i < prevFirstInvisibleRowIndex; i++)
                    rows[i].rowGo.SetActive(false);

            prevFirstVisibleRowIndex = firstVisibleIndex;
            prevFirstInvisibleRowIndex = firstInvisibleIndex;
        }

        private void SetRowIndex(PlayersBackendRow row, int index)
        {
            row.index = index;
            row.rowRect.anchoredPosition = new Vector2(0f, index * negativeRowHeight);
            row.rowGo.SetActive(prevFirstVisibleRowIndex <= index && index < prevFirstInvisibleRowIndex);
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

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChangeDenied)]
        public void OnRPPlayerDataOverriddenDisplayNameChangeDenied()
        {
            if (lockstep.SendingPlayerId != localPlayerId)
                return; // Only the sending player has to reset their latency state back to game state.
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!TryGetRow(rpPlayerData.core.persistentId, out PlayersBackendRow row))
                return; // Some system did something weird.
            row.overriddenDisplayNameField.SetTextWithoutNotify(rpPlayerData.overriddenDisplayName);
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged)]
        public void OnRPPlayerDataOverriddenDisplayNameChanged()
        {
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!TryGetRow(rpPlayerData.core.persistentId, out PlayersBackendRow row))
                return; // Some system did something weird.
            row.sortableOverriddenDisplayName = rpPlayerData.PlayerDisplayName.ToLower();
            row.overriddenDisplayNameField.SetTextWithoutNotify(rpPlayerData.overriddenDisplayName);

            if (currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameAscending)
                || currentSortOrderFunction == nameof(CompareRowOverriddenDisplayNameDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        #endregion

        #region CharacterName

        public void OnCharacterNameChanged(PlayersBackendRow row)
        {
            string inputText = row.characterNameField.text.Trim();
            playersBackendManager.SendSetCharacterNameIA(row.rpPlayerData, inputText);
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataCharacterNameChangeDenied)]
        public void OnRPPlayerDataCharacterNameChangeDenied()
        {
            if (lockstep.SendingPlayerId != localPlayerId)
                return; // Only the sending player has to reset their latency state back to game state.
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!TryGetRow(rpPlayerData.core.persistentId, out PlayersBackendRow row))
                return; // Some system did something weird.
            row.characterNameField.SetTextWithoutNotify(rpPlayerData.characterName);
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged)]
        public void OnRPPlayerDataCharacterNameChanged()
        {
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!TryGetRow(rpPlayerData.core.persistentId, out PlayersBackendRow row))
                return; // Some system did something weird.
            string characterName = rpPlayerData.characterName;
            row.sortableCharacterName = characterName.ToLower();
            row.characterNameField.SetTextWithoutNotify(characterName);

            if (currentSortOrderFunction == nameof(CompareRowCharacterNameAscending)
                || currentSortOrderFunction == nameof(CompareRowCharacterNameDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        #endregion

        #region PermissionGroups

        private bool TryGetPermissionGroupButton(uint permissionGroupId, out PlayersBackendPermissionGroupButton button)
        {
            if (pgButtonsById.TryGetValue(permissionGroupId, out DataToken buttonToken))
            {
                button = (PlayersBackendPermissionGroupButton)buttonToken.Reference;
                return true;
            }
            button = null;
            return false;
        }

        public void OnPermissionGroupClick(PlayersBackendRow row)
        {
            if (lockstep.IsImporting)
                return;
            if (!TryGetPermissionGroupButton(row.permissionsPlayerData.permissionGroup.id, out selectedPermissionGroupButton))
                return;
            selectedPermissionGroupButton.selectedImage.enabled = true;
            selectedRowForPermissionGroupEditing = row;
            selectedRowForPermissionGroupEditing.activeRowHighlightImage.CrossFadeAlpha(1f, 0.1f, ignoreTimeScale: true);

            permissionGroupPopup.SetParent(row.permissionGroupPopupLocation, worldPositionStays: false);
            permissionGroupPopup.anchoredPosition = Vector2.zero;
            Vector2 sizeDelta = permissionGroupPopup.sizeDelta;
            sizeDelta.x = row.permissionGroupRect.rect.width;
            permissionGroupPopup.sizeDelta = sizeDelta;
            menuManager.ShowPopupAtCurrentPosition(
                permissionGroupPopup,
                this,
                nameof(OnPermissionGroupPopupClosed));
        }

        public void OnPermissionGroupPopupClosed()
        {
            permissionGroupPopup.SetParent(popupsParent, worldPositionStays: false);
            permissionGroupPopup.anchoredPosition = Vector2.zero;
            selectedPermissionGroupButton.selectedImage.enabled = false;
            selectedPermissionGroupButton = null;
            selectedRowForPermissionGroupEditing.activeRowHighlightImage.CrossFadeAlpha(0f, 0.1f, ignoreTimeScale: true);
            selectedRowForPermissionGroupEditing = null;
        }

        public void OnPermissionGroupPopupButtonClick(PlayersBackendPermissionGroupButton button)
        {
            // Lazy latency hiding.
            // If the player ends up trying to edit the same player's permission group again before the IA
            // actually runs it's going to show the "wrong" group (the one the player is still apart of in the
            // game state), and it would only update once the IA runs.
            SetPermissionGroupLabelText(selectedRowForPermissionGroupEditing, button.permissionGroup.groupName);
            playersBackendManager.SendSetPlayerPermissionGroupIA(
                selectedRowForPermissionGroupEditing.permissionsPlayerData.core,
                button.permissionGroup);
            menuManager.ClosePopup(permissionGroupPopup, doCallback: true);
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnPlayerPermissionGroupChangeDenied)]
        public void OnPlayerPermissionGroupChangeDenied()
        {
            if (lockstep.SendingPlayerId != localPlayerId)
                return; // Only the sending player has to reset their latency state back to game state.
            PermissionsPlayerData permissionsPlayerData = permissionManager.PlayerDataForEvent;
            if (!TryGetRow(permissionsPlayerData.core.persistentId, out PlayersBackendRow row))
                return; // Some system did something weird.
            SetPermissionGroupLabelText(row, permissionsPlayerData.permissionGroup.groupName);
        }

        [PermissionsEvent(PermissionsEventType.OnPlayerPermissionGroupChanged)]
        public void OnPlayerPermissionGroupChanged()
        {
            PermissionsPlayerData permissionsPlayerData = permissionManager.PlayerDataForEvent;
            if (!TryGetRow(permissionsPlayerData.core.persistentId, out PlayersBackendRow row))
                return; // Some system did something weird.
            SetPermissionGroupLabelText(row, permissionsPlayerData.permissionGroup.groupName);
            PotentiallySortChangedPermissionGroupRow(row);

            if (row != selectedRowForPermissionGroupEditing)
                return;
            selectedPermissionGroupButton.selectedImage.enabled = false;
            if (!TryGetPermissionGroupButton(permissionsPlayerData.permissionGroup.id, out var button))
                menuManager.ClosePopup(permissionGroupPopup, doCallback: true);
            else
            {
                selectedPermissionGroupButton = button;
                selectedPermissionGroupButton.selectedImage.enabled = true;
            }
        }

        private void SetPermissionGroupLabelText(PlayersBackendRow row, string groupName)
        {
            row.sortablePermissionGroupName = groupName.ToLower();
            row.permissionGroupLabel.text = groupName;
        }

        private void PotentiallySortChangedPermissionGroupRow(PlayersBackendRow row)
        {
            if (currentSortOrderFunction == nameof(CompareRowPermissionGroupAscending)
                || currentSortOrderFunction == nameof(CompareRowPermissionGroupDescending))
            {
                UpdateSortPositionDueToValueChange(row);
            }
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupRenamed)]
        public void OnPermissionGroupRenamed()
        {
            PermissionGroup renamedGroup = permissionManager.RenamedPermissionGroup;
            string permissionGroupName = renamedGroup.groupName;
            string sortablePermissionGroupName = permissionGroupName.ToLower();

            if (TryGetPermissionGroupButton(renamedGroup.id, out var button))
                UpdatePermissionGroupButtonLabel(button);

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

            if (affectedCount != 0
                && (currentSortOrderFunction == nameof(CompareRowPermissionGroupAscending)
                    || currentSortOrderFunction == nameof(CompareRowPermissionGroupDescending)))
            {
                UpdateSortPositionsDueToMultipleValueChanges();
            }
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupDuplicated)]
        public void OnPermissionGroupDuplicated()
        {
            var group = permissionManager.CreatedPermissionGroup;
            PlayersBackendPermissionGroupButton button = CreatePermissionGroupButton(group);
            pgButtonsById.Add(group.id, button);
            InsertSortPermissionGroupButton(button);
            CalculatePermissionGroupsPopupHeight();
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupDeleted)]
        public void OnPermissionGroupDeleted()
        {
            if (!TryGetPermissionGroupButton(permissionManager.DeletedPermissionGroup.id, out var button))
                return;
            button.gameObject.SetActive(false);
            button.transform.SetAsLastSibling();
            ArrList.Add(ref unusedPGButtons, ref unusedPGButtonsCount, button);
            ArrList.Remove(ref pgButtons, ref pgButtonsCount, button);
            CalculatePermissionGroupsPopupHeight();
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionManagerImportFinished)]
        public void OnPermissionManagerImportFinished()
        {
            RebuildPermissionGroupButtons();
        }

        private void RebuildPermissionGroupButtons()
        {
            EnsureClosedPermissionGroupPopup();

            int newCount = permissionManager.PermissionGroupsCount;

            pgButtonsById.Clear();
            ArrList.AddRange(ref unusedPGButtons, ref unusedPGButtonsCount, pgButtons, pgButtonsCount);
            if (newCount < pgButtonsCount)
                for (int i = 0; i < pgButtonsCount - newCount; i++)
                {
                    // Disable the low index ones, the higher ones will be reused from the unusedRows "stack".
                    PlayersBackendPermissionGroupButton button = pgButtons[i];
                    button.gameObject.SetActive(false);
                    button.transform.SetAsLastSibling();
                }

            ArrList.Clear(ref pgButtons, ref pgButtonsCount);
            ArrList.EnsureCapacity(ref pgButtons, newCount);

            PermissionGroup[] permissionGroups = permissionManager.PermissionGroupsRaw;
            for (int i = 0; i < newCount; i++)
            {
                PermissionGroup group = permissionGroups[i];
                PlayersBackendPermissionGroupButton button = CreatePermissionGroupButton(group);
                InsertSortPermissionGroupButton(button);
                pgButtonsById.Add(group.id, button);
            }

            CalculatePermissionGroupsPopupHeight();
        }

        private string GetPermissionGroupButtonSortableName(PermissionGroup group)
        {
            return group.isDefault ? "a" : "b" + group.groupName.ToLower();
        }

        private PlayersBackendPermissionGroupButton CreatePermissionGroupButton(PermissionGroup group)
        {
            PlayersBackendPermissionGroupButton button = CreatePermissionGroupButton();
            button.permissionGroup = group;
            button.sortablePermissionGroupName = GetPermissionGroupButtonSortableName(group);
            button.groupNameLabel.text = group.groupName;

            button.gameObject.SetActive(true);
            return button;
        }

        private PlayersBackendPermissionGroupButton CreatePermissionGroupButton()
        {
            if (unusedPGButtonsCount != 0)
                return ArrList.RemoveAt(ref unusedPGButtons, ref unusedPGButtonsCount, unusedPGButtonsCount - 1);
            GameObject go = Instantiate(permissionGroupPrefab);
            go.transform.SetParent(permissionGroupsParent, worldPositionStays: false);
            return go.GetComponent<PlayersBackendPermissionGroupButton>();
        }

        private void UpdatePermissionGroupButtonLabel(PlayersBackendPermissionGroupButton button)
        {
            PermissionGroup group = button.permissionGroup;
            button.sortablePermissionGroupName = GetPermissionGroupButtonSortableName(group);
            button.groupNameLabel.text = group.groupName;
            ArrList.Remove(ref pgButtons, ref pgButtonsCount, button);
            button.transform.SetAsLastSibling();
            InsertSortPermissionGroupButton(button);
        }

        private void InsertSortPermissionGroupButton(PlayersBackendPermissionGroupButton button)
        {
            if (pgButtonsCount == 0)
            {
                button.transform.SetSiblingIndex(permissionGroupButtonSiblingIndexBaseOffset);
                ArrList.Add(ref pgButtons, ref pgButtonsCount, button);
                return;
            }
            int index = pgButtonsCount; // Not -1 because the new row is not in the list yet.
            do
            {
                if (pgButtons[index - 1].sortablePermissionGroupName.CompareTo(button.sortablePermissionGroupName) <= 0)
                    break;
                index--;
            }
            while (index > 0);
            button.transform.SetSiblingIndex(permissionGroupButtonSiblingIndexBaseOffset + index);
            ArrList.Insert(ref pgButtons, ref pgButtonsCount, button, index);
        }

        private void CalculatePermissionGroupsPopupHeight()
        {
            float desiredHeight = pgButtonsCount * permissionGroupButtonHeight;
            Vector2 sizeDelta = permissionGroupPopup.sizeDelta;
            sizeDelta.y = Mathf.Min(maxPermissionGroupsPopupHeight, desiredHeight);
            permissionGroupPopup.sizeDelta = sizeDelta;
            permissionGroupsScrollRect.vertical = desiredHeight > maxPermissionGroupsPopupHeight;
        }

        #endregion

        #region Delete

        public void OnDeleteClick(PlayersBackendRow row)
        {
            if (lockstep.IsImporting)
                return;
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
            playersBackendManager.SendDeleteOfflinePlayerDataIA(toDelete);
            // Don't need to listen to the denied event because row deletion is not latency hidden.
            // Thank goodness.
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

        /// <summary>
        /// <para>Adds <paramref name="row"/> to <see cref="rows"/>.</para>
        /// </summary>
        /// <param name="row"></param>
        private void InsertSortNewRow(PlayersBackendRow row)
        {
            if (rowsCount == 0)
            {
                ArrList.Add(ref rows, ref rowsCount, row);
                currentRowsContentHeight = rowsCount * rowHeight;
                rowsContent.sizeDelta = new Vector2(0f, currentRowsContentHeight);
                SetRowIndex(row, 0);
                return;
            }
            compareRight = row;
            int index = rowsCount; // Not -1 because the new row is not in the list yet.
            do
            {
                compareLeft = rows[index - 1];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                index--;
            }
            while (index > 0);
            ArrList.Insert(ref rows, ref rowsCount, row, index);
            currentRowsContentHeight = rowsCount * rowHeight;
            rowsContent.sizeDelta = new Vector2(0f, currentRowsContentHeight);
            for (int i = index; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
        }

        private void UpdateSortPositionDueToValueChange(PlayersBackendRow row)
        {
            if (!PageIsVisible)
            {
                SortOne(row);
                return;
            }
            if (someRowsAreOutOfSortOrder || IsInSortedPosition(row))
                return;
            MarkAsSomeRowsNoLongerBeingInSortOrder();
        }

        private void UpdateSortPositionsDueToMultipleValueChanges()
        {
            if (!PageIsVisible)
                SortAll();
            else
                MarkAsSomeRowsNoLongerBeingInSortOrder();
        }

        private void MarkAsSomeRowsNoLongerBeingInSortOrder()
        {
            currentSortOrderImage.enabled = false;
            someRowsAreOutOfSortOrder = true;
        }

        private bool IsInSortedPosition(PlayersBackendRow row)
        {
            int index = row.index;

            if (index > 0) // Check if it would move left.
            {
                compareLeft = rows[index - 1];
                compareRight = row;
                SendCustomEvent(currentSortOrderFunction);
                if (!leftSortsFirst)
                    return false;
            }

            if (index < rowsCount - 1) // Check if it would move right.
            {
                compareLeft = row;
                compareRight = rows[index + 1];
                SendCustomEvent(currentSortOrderFunction);
                if (!leftSortsFirst)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// <para><paramref name="row"/> must already be in <see cref="rows"/>.</para>
        /// </summary>
        /// <param name="row"></param>
        private void SortOne(PlayersBackendRow row)
        {
            int index = row.index;
            int initialIndex = index;

            compareRight = row;
            while (index > 0) // Try move left.
            {
                compareLeft = rows[index - 1];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                rows[index] = compareLeft;
                SetRowIndex(compareLeft, index);
                index--;
            }
            if (index != initialIndex)
            {
                rows[index] = row;
                SetRowIndex(row, index);
                return;
            }

            compareLeft = row;
            while (index < rowsCount - 1) // Try move right.
            {
                compareRight = rows[index + 1];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                rows[index] = compareRight;
                SetRowIndex(compareRight, index);
                index++;
            }
            if (index != initialIndex)
            {
                rows[index] = row;
                SetRowIndex(row, index);
                return;
            }
        }

        private void SortAll()
        {
            HideAllCurrentlyVisibleRows();
            MergeSort(currentSortOrderFunction);
            for (int i = 0; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
            someRowsAreOutOfSortOrder = false;
            ShowOnlyRowsVisibleInViewport();
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
