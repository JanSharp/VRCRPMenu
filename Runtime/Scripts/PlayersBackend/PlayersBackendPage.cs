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
        [HideInInspector][SerializeField][SingletonReference] private PermissionsPagesManagerAPI permissionsPagesManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        public PlayersBackendList rowsList;
        public PlayersBackendRow rowPrefabScript;
        public Transform popupsParent;
        public RectTransform confirmDeletePopup;
        public RectTransform permissionGroupPopup;
        public GameObject permissionGroupPrefab;
        public LayoutElement permissionGroupPrefabLayoutElement;
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

        [PermissionDefinitionReference(nameof(editPermissionsPermissionDef))]
        public string editPermissionsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editPermissionsPermissionDef;

        [PermissionDefinitionReference(nameof(deleteOfflinePlayerDataPermissionDef))]
        public string deleteOfflinePlayerDataPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition deleteOfflinePlayerDataPermissionDef;

        /// <summary>
        /// <para><see cref="uint"/> permissionGroupId => <see cref="PlayersBackendPermissionGroupButton"/> button</para>
        /// </summary>
        private DataDictionary pgButtonsById = new DataDictionary();
        private PlayersBackendPermissionGroupButton[] pgButtons = new PlayersBackendPermissionGroupButton[ArrList.MinCapacity];
        private int pgButtonsCount = 0;
        private PlayersBackendPermissionGroupButton[] unusedPGButtons = new PlayersBackendPermissionGroupButton[ArrList.MinCapacity];
        private int unusedPGButtonsCount = 0;

        private PlayersBackendRow selectedRowForPopup;
        private CorePlayerData playerDataAwaitingDeleteConfirmation;
        private PlayersBackendPermissionGroupButton selectedPermissionGroupButton;

        private uint localPlayerId;
        private bool isInitialized = false;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
            permissionGroupButtonHeight = permissionGroupPrefabLayoutElement.preferredHeight;
            maxPermissionGroupsPopupHeight = permissionGroupPopup.sizeDelta.y;
        }

        [PlayerDataEvent(PlayerDataEventType.OnPrePlayerDataManagerInit)]
        public void OnPrePlayerDataManagerInit()
        {
            rowsList.Initialize();
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
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            RebuildPermissionGroupButtons();
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            EnsureClosedPopups();
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            // Debug.Log($"[RPMenuDebug] PlayersBackendPage  OnImportFinishingUp - playerDataManager.IsPartOfCurrentImport: {playerDataManager.IsPartOfCurrentImport}, permissionManager.IsPartOfCurrentImport: {permissionManager.IsPartOfCurrentImport}");

            if (playerDataManager.IsPartOfCurrentImport)
            {
                RebuildRows(); // Runs ShowOnlyRowsVisibleInViewport, do not need to call it manually here.
                if (lockstep.FlaggedToContinueNextFrame)
                    return;
            }

            if (permissionManager.IsPartOfCurrentImport)
            {
                RebuildPermissionGroupButtons();
                // Permission groups depend on player data, therefore there is no need to check if updating
                // the permission group for each player is necessary, as all the rows have been rebuilt anyway.
            }
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
            if (selectedRowForPopup != null)
                menuManager.ClosePopup(permissionGroupPopup, doCallback: true);
        }

        #region PermissionResolution

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            bool displayNameValue = editDisplayNamePermissionDef.valueForLocalPlayer;
            bool characterNameValue = editCharacterNamePermissionDef.valueForLocalPlayer;
            bool permissionGroupValue = editPermissionsPermissionDef.valueForLocalPlayer;
            bool deleteValue = deleteOfflinePlayerDataPermissionDef.valueForLocalPlayer;

            rowsList.SortOnPermissionChange(displayNameValue, characterNameValue, permissionGroupValue);

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

            PlayersBackendRow[] rows = rowsList.Rows;
            int rowsCount = rowsList.RowsCount;
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

        private bool TryGetRow(uint persistentId, out PlayersBackendRow row) => rowsList.TryGetRow(persistentId, out row);

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            if (!isInitialized)
                return;
            rowsList.CreateRow(playerDataManager.PlayerDataForEvent);
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
                // Somebody could delete player data inside of OnImportFinished, but before
                // our handler has ran, thus deleting player data which we are not yet aware of.
                // The rows are about to be rebuilt in that case, so just ignore.
                // Or somebody could create offline player data and somebody decides to delete it in the
                // created event before we receive the created event.
                // (Once the API to create offline player data exists.)
                return;
            }
            rowsList.RemoveRow(row);
        }

        private void RebuildRows()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                EnsureClosedPopups();
            rowsList.RebuildRows();
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
            rowsList.PotentiallySortChangedOverriddenDisplayNameRow(row);
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
            rowsList.PotentiallySortChangedCharacterNameRow(row);
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

        private void SetSelectedRowForPopup(PlayersBackendRow row)
        {
            selectedRowForPopup = row;
            selectedRowForPopup.activeRowHighlightImage.CrossFadeAlpha(1f, 0.1f, ignoreTimeScale: true);
        }

        private void UnsetSelectedRowForPopup()
        {
            selectedRowForPopup.activeRowHighlightImage.CrossFadeAlpha(0f, 0.1f, ignoreTimeScale: true);
            selectedRowForPopup = null;
        }

        public void OnPermissionGroupClick(PlayersBackendRow row)
        {
            if (lockstep.IsImporting)
                return;
            if (!TryGetPermissionGroupButton(row.permissionsPlayerData.permissionGroup.id, out selectedPermissionGroupButton))
                return;
            EnsureClosedConfirmDeletePopup();
            selectedPermissionGroupButton.selectedImage.enabled = true;
            SetSelectedRowForPopup(row);

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
            UnsetSelectedRowForPopup();
        }

        public void OnPermissionGroupPopupButtonClick(PlayersBackendPermissionGroupButton button)
        {
            // Lazy latency hiding.
            // If the player ends up trying to edit the same player's permission group again before the IA
            // actually runs it's going to show the "wrong" group (the one the player is still apart of in the
            // game state), and it would only update once the IA runs.
            SetPermissionGroupLabelText(selectedRowForPopup, button.permissionGroup.groupName);
            permissionsPagesManager.SendSetPlayerPermissionGroupIA(
                selectedRowForPopup.permissionsPlayerData.core,
                button.permissionGroup);
            menuManager.ClosePopup(permissionGroupPopup, doCallback: true);
        }

        [PermissionsPagesEvent(PermissionsPagesEventType.OnPlayerPermissionGroupChangeDenied)]
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
            rowsList.PotentiallySortChangedPermissionGroupRow(row);

            if (row != selectedRowForPopup)
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

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupRenamed)]
        public void OnPermissionGroupRenamed()
        {
            PermissionGroup renamedGroup = permissionManager.RenamedPermissionGroup;
            string permissionGroupName = renamedGroup.groupName;
            string sortablePermissionGroupName = permissionGroupName.ToLower();

            if (TryGetPermissionGroupButton(renamedGroup.id, out var button))
                UpdatePermissionGroupButtonLabel(button);

            int affectedCount = 0;
            PlayersBackendRow[] rows = rowsList.Rows;
            int rowsCount = rowsList.RowsCount;
            for (int i = 0; i < rowsCount; i++)
            {
                PlayersBackendRow row = rows[i];
                if (row.permissionsPlayerData.permissionGroup != renamedGroup)
                    continue;
                affectedCount++;
                row.sortablePermissionGroupName = sortablePermissionGroupName;
                row.permissionGroupLabel.text = permissionGroupName;
            }

            if (affectedCount != 0)
                rowsList.PotentiallySortChangedPermissionGroupRows();
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupDuplicated)]
        public void OnPermissionGroupDuplicated()
        {
            PermissionGroup group = permissionManager.CreatedPermissionGroup;
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
            EnsureClosedPermissionGroupPopup();
            SetSelectedRowForPopup(row);
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
            UnsetSelectedRowForPopup();
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
    }
}
