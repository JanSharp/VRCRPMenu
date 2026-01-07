using System.Text.RegularExpressions;
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
        public TMP_InputField groupNameField;
        public TextMeshProUGUI playersInGroupCountsText;
        private string playersInGroupCountsFormat;

        public Transform popupsParent;
        public RectTransform wouldLoseEditPermissionsDueToEditPopup;

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
        private bool isInitialized = false;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
            permissionGroupToggleHeight = permissionGroupPrefabLayoutElement.preferredHeight;
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            InitPlayersInGroupCounts();
            RebuildPermissionGroupToggles();
            SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            InitPlayersInGroupCounts();
            RebuildPermissionGroupToggles();
            SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            if (wouldLoseEditPermissionsDueToEditPopup.parent != popupsParent)
                menuManager.ClosePopup(wouldLoseEditPermissionsDueToEditPopup, doCallback: true);
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            if (permissionManager.IsPartOfCurrentImport)
            {
                RebuildPermissionGroupToggles();
                if (activePermissionGroupToggle.permissionGroup == null || activePermissionGroupToggle.permissionGroup.isDeleted)
                    SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
                else
                    UpdatePermissionGroupDetailsExceptNameField();
            }
        }

        #region PlayersInGroup

        private void InitPlayersInGroupCounts()
        {
            playersInGroupCountsFormat = playersInGroupCountsText.text;
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
            deleteGroupButton.interactable = isNotDefault;

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

        private PermissionsPermissionGroupToggle GetPermissionGroupToggle(PermissionGroup permissionGroup)
        {
            return (PermissionsPermissionGroupToggle)pgTogglesById[permissionGroup.id].Reference;
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
            if (TryGetPermissionGroupToggle(renamedGroup.id, out var toggle))
                UpdatePermissionGroupToggleLabel(toggle);

            if (renamedGroup == activePermissionGroupToggle.permissionGroup)
                groupNameField.SetTextWithoutNotify(renamedGroup.groupName);
        }

        [PermissionsPagesEvent(PermissionsPagesEventType.OnPermissionGroupRenameDenied)]
        public void OnPermissionGroupRenameDenied()
        {
            if (!isInitialized)
                return;
            if (lockstep.SendingPlayerId != localPlayerId)
                return; // Only the sending player has to reset their latency state back to game state.
            PermissionGroup group = permissionsPagesManager.PermissionGroupAttemptedToBeAffected;
            if (group == activePermissionGroupToggle.permissionGroup)
                groupNameField.SetTextWithoutNotify(group.groupName);
        }

        public void OnDuplicateClick()
        {
            // Group 0 is the entire matching string. 1 is the first user defined group.
            string prefix = Regex.Match(activePermissionGroupToggle.permissionGroup.groupName, @"^(.*?)(\s+\d+)?$").Groups[1].Value + " ";
            int postfix = 1;
            while (permissionManager.GetPermissionGroup(prefix + postfix) != null)
                postfix++;
            permissionsPagesManager.SendDuplicatePermissionGroupIA(prefix + postfix, activePermissionGroupToggle.permissionGroup);
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

            // TODO: Knowing which player, if any, created a new group would be useful, so we can only
            // switch to that group if it was the local player creating it.
            // Cannot use lockstep.SendingPlayerID because it is not guaranteed that we are inside of an input
            // action here, just that it is game state safe.
            // In other words, the API would have to provide explicit support for knowing which player created
            // the group.
            SetActivePermissionGroupToggle(toggle);
        }

        public void OnDeleteClick()
        {
            permissionsPagesManager.SendDeletePermissionGroupIA(activePermissionGroupToggle.permissionGroup, permissionManager.DefaultPermissionGroup);
        }

        [PermissionsEvent(PermissionsEventType.OnPermissionGroupDeleted)]
        public void OnPermissionGroupDeleted()
        {
            if (!isInitialized)
                return;
            if (!TryGetPermissionGroupToggle(permissionManager.DeletedPermissionGroup.id, out var toggle))
                return;
            toggle.gameObject.SetActive(false);
            toggle.transform.SetAsLastSibling();
            ArrList.Add(ref unusedPGToggles, ref unusedPGTogglesCount, toggle);
            ArrList.Remove(ref pgToggles, ref pgTogglesCount, toggle);
            CalculatePermissionGroupsContentHeight();

            if (toggle == activePermissionGroupToggle)
                SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
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
            if (!isInitialized)
                return;

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
            if (newCount < pgTogglesCount)
                for (int i = 0; i < pgTogglesCount - newCount; i++)
                {
                    // Disable the low index ones, the higher ones will be reused from the unusedRows "stack".
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
            int index = pgTogglesCount; // Not -1 because the new row is not in the list yet.
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
