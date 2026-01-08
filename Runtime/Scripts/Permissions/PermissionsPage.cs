using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PermissionsPage : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;
        [HideInInspector][SerializeField][SingletonReference] private PermissionsPagesManagerAPI permissionsPagesManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        /// <summary>
        /// <para>The order matches that of <see cref="PermissionManagerAPI.PermissionDefinitions"/>.</para>
        /// </summary>
        [HideInInspector] public PermissionsPermissionRow[] permissionRows;

        public RectTransform permissionGroupContent;
        public GameObject permissionGroupPrefab;
        public LayoutElement permissionGroupPrefabLayoutElement;
        public Transform permissionGroupsParent;
        [Min(0)]
        public int permissionGroupToggleSiblingIndexBaseOffset;
        private float permissionGroupToggleHeight;

        public Button deleteGroupButton;
        public Selectable deleteGroupButtonLabel;
        public TMP_InputField groupNameField;
        public Selectable groupNameFieldText;
        public TextMeshProUGUI playersInGroupCountsText;
        private string playersInGroupCountsFormat;

        public Transform popupsParent;
        public RectTransform deleteConfirmationPopup;
        public TextMeshProUGUI deleteConfirmationInfoText;
        private string deleteConfirmationInfoFormat;
        private PermissionGroup groupAwaitingDeleteConfirmation;
        public RectTransform wouldLoseEditPermissionsDueToEditPopup;
        public RectTransform wouldLoseEditPermissionsDueToDeletionPopup;

        /// <summary>
        /// <para><see cref="uint"/> permissionGroupId => <see cref="PermissionsPermissionGroupToggle"/> toggle</para>
        /// </summary>
        private DataDictionary pgTogglesById = new DataDictionary();
        private PermissionsPermissionGroupToggle[] pgToggles = new PermissionsPermissionGroupToggle[ArrList.MinCapacity];
        private int pgTogglesCount = 0;
        private PermissionsPermissionGroupToggle[] unusedPGToggles = new PermissionsPermissionGroupToggle[ArrList.MinCapacity];
        private int unusedPGTogglesCount = 0;
        private PermissionsPermissionGroupToggle activePermissionGroupToggle;

        private uint localPlayerId;
        /// <summary>
        /// <para>Since this is set to <see langword="true"/> in <see cref="LockstepEventType.OnInit"/> and
        /// not <see cref="PlayerDataEventType.OnPrePlayerDataManagerInit"/> the majority of event listeners
        /// end up having to check if <see cref="isInitialized"/> is <see langword="true"/> before doing
        /// anything, since the player data system can raise events before this script gets
        /// <see cref="LockstepEventType.OnInit"/>.</para>
        /// </summary>
        private bool isInitialized = false;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
            permissionGroupToggleHeight = permissionGroupPrefabLayoutElement.preferredHeight;
            InitPlayersInGroupCounts();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            RebuildPermissionGroupToggles();
            SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup.id));
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            RebuildPermissionGroupToggles();
            SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup.id));
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            if (groupAwaitingDeleteConfirmation != null)
                menuManager.ClosePopup(deleteConfirmationPopup, doCallback: true);
            if (wouldLoseEditPermissionsDueToEditPopup.parent != popupsParent)
                menuManager.ClosePopup(wouldLoseEditPermissionsDueToEditPopup, doCallback: true);
            if (wouldLoseEditPermissionsDueToDeletionPopup.parent != popupsParent)
                menuManager.ClosePopup(wouldLoseEditPermissionsDueToDeletionPopup, doCallback: true);
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            if (permissionManager.IsPartOfCurrentImport)
            {
                RebuildPermissionGroupToggles();
                if (activePermissionGroupToggle.permissionGroup == null || activePermissionGroupToggle.permissionGroup.isDeleted)
                    SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup.id));
                else
                {
                    // Permission groups never get renamed during imports, only created or deleted, so this
                    // does not need to change the input field text value.
                    UpdatePermissionGroupDetailsExceptNameField();
                }
            }
        }

        #region PlayersInGroup

        private void InitPlayersInGroupCounts()
        {
            playersInGroupCountsFormat = playersInGroupCountsText.text;
            deleteConfirmationInfoFormat = deleteConfirmationInfoText.text;
        }

        private void UpdatePlayersInGroupCounts()
        {
            PermissionGroup group = activePermissionGroupToggle.permissionGroup;
            playersInGroupCountsText.text = string.Format(playersInGroupCountsFormat, group.playersInGroupCount, group.onlinePlayersInGroupCount);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            if (!isInitialized)
                return;
            UpdatePlayersInGroupCounts();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            if (!isInitialized)
                return;
            UpdatePlayersInGroupCounts();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline()
        {
            if (!isInitialized)
                return;
            UpdatePlayersInGroupCounts();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline()
        {
            if (!isInitialized)
                return;
            UpdatePlayersInGroupCounts();
        }

        [PermissionsEvent(PermissionsEventType.OnPlayerPermissionGroupChanged)]
        public void OnPlayerPermissionGroupChanged()
        {
            if (!isInitialized)
                return;
            UpdatePlayersInGroupCounts();
        }

        #endregion

        #region PermissionGroupToggles

        public void SetActivePermissionGroupToggle(PermissionsPermissionGroupToggle toggle)
        {
            PermissionsPermissionGroupToggle prevToggle = activePermissionGroupToggle;
            activePermissionGroupToggle = toggle;
            if (prevToggle != null)
                prevToggle.toggle.SetIsOnWithoutNotify(false);
            toggle.toggle.SetIsOnWithoutNotify(true);

            PermissionGroup group = toggle.permissionGroup;
            groupNameField.text = group.groupName;
            bool isNotDefault = !group.isDefault;
            groupNameField.interactable = isNotDefault;
            groupNameFieldText.interactable = isNotDefault;
            deleteGroupButton.interactable = isNotDefault;
            deleteGroupButtonLabel.interactable = isNotDefault;

            UpdatePermissionGroupDetailsExceptNameField();
        }

        private void UpdatePermissionGroupDetailsExceptNameField()
        {
            UpdatePlayersInGroupCounts();
            UpdateAllPermissionRows();
        }

        private void UpdateAllPermissionRows()
        {
            int defsCount = permissionManager.PermissionDefinitions.Length;
            bool[] groupValues = activePermissionGroupToggle.permissionGroup.permissionValues;
            for (int i = 0; i < defsCount; i++)
                permissionRows[i].SetIsOnWithoutNotify(groupValues[i]);
        }

        private PermissionsPermissionGroupToggle GetPermissionGroupToggle(uint permissionGroupId)
        {
            return (PermissionsPermissionGroupToggle)pgTogglesById[permissionGroupId].Reference;
        }

        // NOTE: This has a lot of copy paste from PlayersBackendPage.cs

        private bool TryGetPermissionGroupToggle(uint permissionGroupId, out PermissionsPermissionGroupToggle toggle)
        {
            if (pgTogglesById.TryGetValue(permissionGroupId, out DataToken toggleToken))
            {
                toggle = (PermissionsPermissionGroupToggle)toggleToken.Reference;
                return true;
            }
            toggle = null;
            return false;
        }

        public void OnGroupNameChanged()
        {
            permissionsPagesManager.SendRenamePermissionGroupIA(activePermissionGroupToggle.permissionGroup, groupNameField.text);
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupRenamed)]
        public void OnPermissionGroupRenamed()
        {
            if (!isInitialized)
                return;
            PermissionGroup renamedGroup = permissionManager.RenamedPermissionGroup;
            if (TryGetPermissionGroupToggle(renamedGroup.id, out PermissionsPermissionGroupToggle toggle))
                UpdatePermissionGroupToggleLabel(toggle);

            if (renamedGroup == activePermissionGroupToggle.permissionGroup)
                groupNameField.SetTextWithoutNotify(renamedGroup.groupName);

            if (renamedGroup == groupAwaitingDeleteConfirmation)
                UpdateDeleteConfirmationInfoText();
        }

        [PermissionsPagesEvent(PermissionsPagesEventType.OnPermissionGroupRenameDenied)]
        public void OnPermissionGroupRenameDenied()
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            if (lockstep.SendingPlayerId != localPlayerId)
                return; // Only the sending player has to reset their latency state back to game state.
            PermissionGroup group = permissionsPagesManager.PermissionGroupAttemptedToBeAffected;
            if (group == activePermissionGroupToggle.permissionGroup)
                groupNameField.SetTextWithoutNotify(group.groupName);
        }

        public void OnDuplicateClick()
        {
            PermissionGroup group = activePermissionGroupToggle.permissionGroup;
            string groupName = permissionManager.GetFirstUnusedGroupName(group.groupName);
            permissionsPagesManager.SendDuplicatePermissionGroupIA(groupName, group);
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupDuplicated)]
        public void OnPermissionGroupDuplicated()
        {
            if (!isInitialized)
                return;
            PermissionGroup group = permissionManager.CreatedPermissionGroup;
            PermissionsPermissionGroupToggle toggle = CreatePermissionGroupToggle(group);
            pgTogglesById.Add(group.id, toggle);
            InsertSortPermissionGroupToggle(toggle);
            CalculatePermissionGroupsContentHeight();

            CorePlayerData player = permissionManager.PlayerDataCreatingPermissionGroup;
            if (player != null && player.isLocal)
                SetActivePermissionGroupToggle(toggle);
        }

        public void OnDeleteClick()
        {
            groupAwaitingDeleteConfirmation = activePermissionGroupToggle.permissionGroup;
            UpdateDeleteConfirmationInfoText();
            menuManager.ShowPopupAtItsAnchor(deleteConfirmationPopup, this, nameof(OnDeleteConfirmationPopupClosed));
        }

        private void UpdateDeleteConfirmationInfoText()
        {
            deleteConfirmationInfoText.text = string.Format(deleteConfirmationInfoFormat, groupAwaitingDeleteConfirmation.groupName);
        }

        public void OnDeleteConfirmationPopupClosed()
        {
            deleteConfirmationPopup.SetParent(popupsParent, worldPositionStays: false);
            groupAwaitingDeleteConfirmation = null;
        }

        public void OnConfirmDeleteClick()
        {
            if (groupAwaitingDeleteConfirmation == null || groupAwaitingDeleteConfirmation.isDeleted)
                return;
            permissionsPagesManager.SendDeletePermissionGroupIA(groupAwaitingDeleteConfirmation, permissionManager.DefaultPermissionGroup);
            menuManager.ClosePopup(deleteConfirmationPopup, doCallback: true); // Clears groupAwaitingDeleteConfirmation.
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupDeleted)]
        public void OnPermissionGroupDeleted()
        {
            if (!isInitialized)
                return;
            PermissionGroup deletedGroup = permissionManager.DeletedPermissionGroup;
            if (deletedGroup == groupAwaitingDeleteConfirmation)
                menuManager.ClosePopup(deleteConfirmationPopup, doCallback: true);
            if (!TryGetPermissionGroupToggle(permissionManager.DeletedPermissionGroup.id, out PermissionsPermissionGroupToggle toggle))
                return;
            toggle.gameObject.SetActive(false);
            toggle.transform.SetAsLastSibling();
            ArrList.Add(ref unusedPGToggles, ref unusedPGTogglesCount, toggle);
            ArrList.Remove(ref pgToggles, ref pgTogglesCount, toggle);
            CalculatePermissionGroupsContentHeight();

            if (toggle == activePermissionGroupToggle)
                SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup.id));
        }

        [PermissionsPagesEvent(PermissionsPagesEventType.OnPermissionGroupDeletedDenied)]
        public void OnPermissionGroupDeletedDenied()
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            if (lockstep.SendingPlayerId != localPlayerId || !permissionsPagesManager.WouldLoseEditPermissions)
                return;
            menuManager.ShowPopupAtItsAnchor(
                wouldLoseEditPermissionsDueToDeletionPopup,
                this,
                nameof(OnWouldLoseEditPermissionsDueToDeletionPopupClosed));
        }

        public void OnWouldLoseEditPermissionsDueToDeletionPopupClosed()
        {
            wouldLoseEditPermissionsDueToDeletionPopup.SetParent(popupsParent, worldPositionStays: false);
        }

        public void OnRowValueChanged(PermissionsPermissionRow row)
        {
            permissionsPagesManager.SendSetPermissionValueIA(
                activePermissionGroupToggle.permissionGroup,
                row.permissionDef,
                row.toggle.isOn);
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionValueChanged)]
        public void OnPermissionValueChanged()
        {
            if (!isInitialized)
                return;
            PermissionGroup group = permissionManager.ChangedPermissionGroup;
            if (group != activePermissionGroupToggle.permissionGroup)
                return;
            int index = permissionManager.ChangedPermission.index;
            permissionRows[index].SetIsOnWithoutNotify(group.permissionValues[index]);
        }

        [PermissionsPagesEvent(PermissionsPagesEventType.OnPermissionValueChangeDenied)]
        public void OnPermissionValueChangeDenied()
        {
            // No need for a isInitialized check, this can only trigger through an input action, not any GS safe context.

            if (lockstep.SendingPlayerId == localPlayerId && permissionsPagesManager.WouldLoseEditPermissions)
                menuManager.ShowPopupAtItsAnchor(
                    wouldLoseEditPermissionsDueToEditPopup,
                    this,
                    nameof(OnWouldLoseEditPermissionsDueToEditPopupClosed));

            PermissionGroup group = permissionsPagesManager.PermissionGroupAttemptedToBeAffected;
            if (group != activePermissionGroupToggle.permissionGroup)
                return;
            int index = permissionsPagesManager.PermissionAttemptedToBeAffected.index;
            permissionRows[index].SetIsOnWithoutNotify(group.permissionValues[index]);
        }

        public void OnWouldLoseEditPermissionsDueToEditPopupClosed()
        {
            wouldLoseEditPermissionsDueToEditPopup.SetParent(popupsParent, worldPositionStays: false);
        }

        private void RebuildPermissionGroupToggles()
        {
            int newCount = permissionManager.PermissionGroupsCount;
            PermissionGroup activeGroup = activePermissionGroupToggle == null
                ? null
                : activePermissionGroupToggle.permissionGroup;

            pgTogglesById.Clear();
            ArrList.AddRange(ref unusedPGToggles, ref unusedPGTogglesCount, pgToggles, pgTogglesCount);
            for (int i = 0; i < pgTogglesCount - newCount; i++)
            {
                // Disable the low index ones, the higher ones will be reused from the unusedPGToggles "stack".
                PermissionsPermissionGroupToggle toggle = pgToggles[i];
                toggle.gameObject.SetActive(false);
                toggle.toggle.SetIsOnWithoutNotify(false);
                toggle.transform.SetAsLastSibling();
            }

            ArrList.Clear(ref pgToggles, ref pgTogglesCount);
            ArrList.EnsureCapacity(ref pgToggles, newCount);

            PermissionGroup[] permissionGroups = permissionManager.PermissionGroupsRaw;
            for (int i = 0; i < newCount; i++)
            {
                PermissionGroup group = permissionGroups[i];
                PermissionsPermissionGroupToggle toggle = CreatePermissionGroupToggle(group);
                InsertSortPermissionGroupToggle(toggle);
                pgTogglesById.Add(group.id, toggle);

                bool isActiveToggle = group == activeGroup;
                toggle.toggle.SetIsOnWithoutNotify(isActiveToggle);
                if (isActiveToggle)
                    activePermissionGroupToggle = toggle;
            }

            CalculatePermissionGroupsContentHeight();
        }

        private string GetPermissionGroupToggleSortableName(PermissionGroup group)
        {
            return group.isDefault ? "a" : "b" + group.groupName.ToLower();
        }

        private PermissionsPermissionGroupToggle CreatePermissionGroupToggle(PermissionGroup group)
        {
            PermissionsPermissionGroupToggle toggle = CreatePermissionGroupToggle();
            toggle.permissionGroup = group;
            toggle.sortablePermissionGroupName = GetPermissionGroupToggleSortableName(group);
            toggle.groupNameLabel.text = group.groupName;

            toggle.gameObject.SetActive(true);
            return toggle;
        }

        private PermissionsPermissionGroupToggle CreatePermissionGroupToggle()
        {
            if (unusedPGTogglesCount != 0)
                return ArrList.RemoveAt(ref unusedPGToggles, ref unusedPGTogglesCount, unusedPGTogglesCount - 1);
            GameObject go = Instantiate(permissionGroupPrefab);
            go.transform.SetParent(permissionGroupsParent, worldPositionStays: false);
            return go.GetComponent<PermissionsPermissionGroupToggle>();
        }

        private void UpdatePermissionGroupToggleLabel(PermissionsPermissionGroupToggle toggle)
        {
            PermissionGroup group = toggle.permissionGroup;
            toggle.sortablePermissionGroupName = GetPermissionGroupToggleSortableName(group);
            toggle.groupNameLabel.text = group.groupName;
            ArrList.Remove(ref pgToggles, ref pgTogglesCount, toggle);
            toggle.transform.SetAsLastSibling();
            InsertSortPermissionGroupToggle(toggle);
        }

        private void InsertSortPermissionGroupToggle(PermissionsPermissionGroupToggle toggle)
        {
            if (pgTogglesCount == 0)
            {
                toggle.transform.SetSiblingIndex(permissionGroupToggleSiblingIndexBaseOffset);
                ArrList.Add(ref pgToggles, ref pgTogglesCount, toggle);
                return;
            }
            int index = pgTogglesCount; // Not -1 because the toggle is not in the list yet.
            do
            {
                if (pgToggles[index - 1].sortablePermissionGroupName.CompareTo(toggle.sortablePermissionGroupName) <= 0)
                    break;
                index--;
            }
            while (index > 0);
            toggle.transform.SetSiblingIndex(permissionGroupToggleSiblingIndexBaseOffset + index);
            ArrList.Insert(ref pgToggles, ref pgTogglesCount, toggle, index);
        }

        private void CalculatePermissionGroupsContentHeight()
        {
            Vector2 sizeDelta = permissionGroupContent.sizeDelta;
            sizeDelta.y = pgTogglesCount * permissionGroupToggleHeight;
            permissionGroupContent.sizeDelta = sizeDelta;
        }

        #endregion
    }
}
