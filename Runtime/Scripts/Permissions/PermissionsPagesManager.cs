using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(PermissionsPagesEventAttribute), typeof(PermissionsPagesEventType))]
    public class PermissionsPagesManager : PermissionsPagesManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;

        [PermissionDefinitionReference(nameof(editPermissionsPDef))]
        public string editPermissionsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editPermissionsPDef;

        public override void SendDuplicatePermissionGroupIA(string groupName, PermissionGroup toDuplicate)
        {
            lockstep.WriteString(groupName);
            lockstep.WriteSmallUInt(toDuplicate.id);
            lockstep.SendInputAction(duplicatePermissionGroupIAId);
        }

        [HideInInspector][SerializeField] private uint duplicatePermissionGroupIAId;
        [LockstepInputAction(nameof(duplicatePermissionGroupIAId))]
        public void OnDuplicatePermissionGroupIA()
        {
            CorePlayerData sendingPlayerData = playerDataManager.SendingPlayerData;
            if (!permissionManager.PlayerHasPermission(sendingPlayerData, editPermissionsPDef))
                return;
            string groupName = lockstep.ReadString();
            uint groupId = lockstep.ReadSmallUInt();
            if (!permissionManager.TryGetPermissionGroup(groupId, out PermissionGroup group))
                return;
            permissionManager.DuplicatePermissionGroupInGS(groupName, group, sendingPlayerData);
        }

        public override void SendDeletePermissionGroupIA(PermissionGroup group, PermissionGroup groupToMovePlayersTo)
        {
            lockstep.WriteSmallUInt(group.id);
            lockstep.WriteSmallUInt(groupToMovePlayersTo.id);
            lockstep.SendInputAction(deletePermissionGroupIAId);
        }

        [HideInInspector][SerializeField] private uint deletePermissionGroupIAId;
        [LockstepInputAction(nameof(deletePermissionGroupIAId))]
        public void OnDeletePermissionGroupIA()
        {
            uint groupId = lockstep.ReadSmallUInt();
            if (!permissionManager.TryGetPermissionGroup(groupId, out PermissionGroup group))
                return;

            CorePlayerData sendingPlayerData = playerDataManager.SendingPlayerData;
            if (!permissionManager.PlayerHasPermission(sendingPlayerData, editPermissionsPDef))
            {
                RaiseOnPermissionGroupDeletedDenied(group, wouldLoseEditPermissions: false);
                return;
            }

            uint groupToMovePlayersToId = lockstep.ReadSmallUInt();
            if (!permissionManager.TryGetPermissionGroup(groupToMovePlayersToId, out PermissionGroup groupToMovePlayersTo))
                return;

            if (permissionManager.GetPermissionsPlayerData(sendingPlayerData).permissionGroup != group
                || groupToMovePlayersTo.permissionValues[editPermissionsPDef.index])
            {
                permissionManager.DeletePermissionGroupInGS(group, groupToMovePlayersTo);
            }
            else
                RaiseOnPermissionGroupDeletedDenied(group, wouldLoseEditPermissions: true);
        }

        public override void SendRenamePermissionGroupIA(PermissionGroup group, string newGroupName)
        {
            lockstep.WriteSmallUInt(group.id);
            lockstep.WriteString(newGroupName);
            lockstep.SendInputAction(renamePermissionGroupIAId);
        }

        [HideInInspector][SerializeField] private uint renamePermissionGroupIAId;
        [LockstepInputAction(nameof(renamePermissionGroupIAId))]
        public void OnRenamePermissionGroupIA()
        {
            uint groupId = lockstep.ReadSmallUInt();
            if (!permissionManager.TryGetPermissionGroup(groupId, out PermissionGroup group))
                return;

            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editPermissionsPDef))
            {
                RaiseOnPermissionGroupRenameDenied(group);
                return;
            }

            string newGroupName = lockstep.ReadString();
            permissionManager.RenamePermissionGroupInGS(group, newGroupName);
        }

        public override void SendSetPlayerPermissionGroupIA(CorePlayerData corePlayerData, PermissionGroup group)
        {
            lockstep.WriteSmallUInt(corePlayerData.persistentId); // playerId would not work for offline players.
            lockstep.WriteSmallUInt(group.id);
            lockstep.SendInputAction(setPlayerPermissionGroupIAId);
        }

        [HideInInspector][SerializeField] private uint setPlayerPermissionGroupIAId;
        [LockstepInputAction(nameof(setPlayerPermissionGroupIAId))]
        public void OnSetPlayerPermissionGroupIA()
        {
            uint persistentId = lockstep.ReadSmallUInt();

            CorePlayerData sendingPlayerData = playerDataManager.SendingPlayerData;
            if (!permissionManager.PlayerHasPermission(sendingPlayerData, editPermissionsPDef))
            {
                RaiseOnPlayerPermissionGroupChangeDenied(persistentId, wouldLoseEditPermissions: false);
                return;
            }

            if (!playerDataManager.TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData corePlayerData))
                return;
            uint groupId = lockstep.ReadSmallUInt();
            if (!permissionManager.TryGetPermissionGroup(groupId, out PermissionGroup group))
                return;

            if (corePlayerData != sendingPlayerData || group.permissionValues[editPermissionsPDef.index])
                permissionManager.SetPlayerPermissionGroupInGS(corePlayerData, group);
            else
                RaiseOnPlayerPermissionGroupChangeDenied(persistentId, wouldLoseEditPermissions: true);
        }

        public override void SendSetPermissionValueIA(PermissionGroup group, PermissionDefinition permissionDef, bool value)
        {
            lockstep.WriteSmallUInt(group.id);
            lockstep.WriteSmallUInt((uint)permissionDef.index);
            lockstep.WriteFlags(value);
            lockstep.SendInputAction(setPermissionValueIAId);
        }

        [HideInInspector][SerializeField] private uint setPermissionValueIAId;
        [LockstepInputAction(nameof(setPermissionValueIAId))]
        public void OnSetPermissionValueIA()
        {
            uint groupId = lockstep.ReadSmallUInt();
            if (!permissionManager.TryGetPermissionGroup(groupId, out PermissionGroup group))
                return;
            int defIndex = (int)lockstep.ReadSmallUInt();
            PermissionDefinition permissionDef = permissionManager.PermissionDefinitions[defIndex];

            CorePlayerData sendingPlayerData = playerDataManager.SendingPlayerData;
            if (!permissionManager.PlayerHasPermission(sendingPlayerData, editPermissionsPDef))
            {
                RaiseOnPermissionValueChangeDenied(group, permissionDef, wouldLoseEditPermissions: false);
                return;
            }

            lockstep.ReadFlags(out bool value);
            if (value
                || permissionDef != editPermissionsPDef
                || group != permissionManager.GetPermissionsPlayerData(sendingPlayerData).permissionGroup)
            {
                permissionManager.SetPermissionValueInGS(group, permissionDef, value);
            }
            else
                RaiseOnPermissionValueChangeDenied(group, permissionDef, wouldLoseEditPermissions: true);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPermissionGroupDeletedDeniedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPermissionGroupRenameDeniedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerPermissionGroupChangeDeniedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPermissionValueChangeDeniedListeners;

        private bool wouldLoseEditPermissions;
        public override bool WouldLoseEditPermissions => wouldLoseEditPermissions;
        private PermissionGroup permissionGroupAttemptedToBeAffected;
        public override PermissionGroup PermissionGroupAttemptedToBeAffected => permissionGroupAttemptedToBeAffected;
        private uint persistentIdAttemptedToBeAffected;
        public override uint PersistentIdAttemptedToBeAffected => persistentIdAttemptedToBeAffected;
        private PermissionDefinition permissionAttemptedToBeAffected;
        public override PermissionDefinition PermissionAttemptedToBeAffected => permissionAttemptedToBeAffected;

        private void RaiseOnPermissionGroupDeletedDenied(PermissionGroup permissionGroupAttemptedToBeAffected, bool wouldLoseEditPermissions)
        {
            this.permissionGroupAttemptedToBeAffected = permissionGroupAttemptedToBeAffected;
            this.wouldLoseEditPermissions = wouldLoseEditPermissions;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPermissionGroupDeletedDeniedListeners, nameof(PermissionsPagesEventType.OnPermissionGroupDeletedDenied));
            this.permissionGroupAttemptedToBeAffected = null; // To prevent misuse of the API.
            this.wouldLoseEditPermissions = false; // To prevent misuse of the API.
        }

        private void RaiseOnPermissionGroupRenameDenied(PermissionGroup permissionGroupAttemptedToBeAffected)
        {
            this.permissionGroupAttemptedToBeAffected = permissionGroupAttemptedToBeAffected;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPermissionGroupRenameDeniedListeners, nameof(PermissionsPagesEventType.OnPermissionGroupRenameDenied));
            this.permissionGroupAttemptedToBeAffected = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerPermissionGroupChangeDenied(uint persistentIdAttemptedToBeAffected, bool wouldLoseEditPermissions)
        {
            this.persistentIdAttemptedToBeAffected = persistentIdAttemptedToBeAffected;
            this.wouldLoseEditPermissions = wouldLoseEditPermissions;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerPermissionGroupChangeDeniedListeners, nameof(PermissionsPagesEventType.OnPlayerPermissionGroupChangeDenied));
            this.persistentIdAttemptedToBeAffected = 0u; // To prevent misuse of the API.
            this.wouldLoseEditPermissions = false; // To prevent misuse of the API.
        }

        private void RaiseOnPermissionValueChangeDenied(PermissionGroup permissionGroupAttemptedToBeAffected, PermissionDefinition permissionAttemptedToBeAffected, bool wouldLoseEditPermissions)
        {
            this.permissionGroupAttemptedToBeAffected = permissionGroupAttemptedToBeAffected;
            this.permissionAttemptedToBeAffected = permissionAttemptedToBeAffected;
            this.wouldLoseEditPermissions = wouldLoseEditPermissions;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPermissionValueChangeDeniedListeners, nameof(PermissionsPagesEventType.OnPermissionValueChangeDenied));
            this.permissionGroupAttemptedToBeAffected = null; // To prevent misuse of the API.
            this.permissionAttemptedToBeAffected = null; // To prevent misuse of the API.
            this.wouldLoseEditPermissions = false; // To prevent misuse of the API.
        }

        #endregion
    }
}
