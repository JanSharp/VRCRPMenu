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
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;

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
            RebuildPermissionGroupToggles();
            SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            RebuildPermissionGroupToggles();
            SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            if (permissionManager.IsPartOfCurrentImport)
            {
                RebuildPermissionGroupToggles();
                if (activePermissionGroupToggle.permissionGroup == null || activePermissionGroupToggle.permissionGroup.isDeleted)
                    SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
            }
        }

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

            int defsCount = permissionManager.PermissionDefinitions.Length;
            bool[] groupValues = group.permissionValues;
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

            if (permissionManager.DeletedPermissionGroup == activePermissionGroupToggle)
                SetActivePermissionGroupToggle(GetPermissionGroupToggle(permissionManager.DefaultPermissionGroup));
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
