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

        [PermissionDefinitionReference(nameof(editPermissionsPermissionDef))]
        public string editPermissionsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editPermissionsPermissionDef;

        [PermissionDefinitionReference(nameof(editPermissionGroupPermissionDef))]
        public string editPermissionGroupPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editPermissionGroupPermissionDef;

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

            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editPermissionsPermissionDef))
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

            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editPermissionGroupPermissionDef))
            {
                RaiseOnPlayerPermissionGroupChangeDenied(persistentId);
                return;
            }

            if (!playerDataManager.TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData corePlayerData))
                return;
            uint groupId = lockstep.ReadSmallUInt();
            if (!permissionManager.TryGetPermissionGroup(groupId, out PermissionGroup group))
                return;
            permissionManager.SetPlayerPermissionGroupInGS(corePlayerData, group);
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

            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editPermissionsPermissionDef))
            {
                RaiseOnPermissionValueChangeDenied(group, permissionDef);
                return;
            }

            lockstep.ReadFlags(out bool value);
            permissionManager.SetPermissionValueInGS(group, permissionDef, value);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPermissionGroupRenameDeniedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerPermissionGroupChangeDeniedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPermissionValueChangeDeniedListeners;

        private PermissionGroup permissionGroupAttemptedToBeAffected;
        public override PermissionGroup PermissionGroupAttemptedToBeAffected => permissionGroupAttemptedToBeAffected;
        private uint persistentIdAttemptedToBeAffected;
        public override uint PersistentIdAttemptedToBeAffected => persistentIdAttemptedToBeAffected;
        private PermissionDefinition permissionAttemptedToBeAffected;
        public override PermissionDefinition PermissionAttemptedToBeAffected => permissionAttemptedToBeAffected;

        private void RaiseOnPermissionGroupRenameDenied(PermissionGroup permissionGroupAttemptedToBeAffected)
        {
            this.permissionGroupAttemptedToBeAffected = permissionGroupAttemptedToBeAffected;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPermissionGroupRenameDeniedListeners, nameof(PermissionsPagesEventType.OnPermissionGroupRenameDenied));
            this.permissionGroupAttemptedToBeAffected = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerPermissionGroupChangeDenied(uint persistentIdAttemptedToBeAffected)
        {
            this.persistentIdAttemptedToBeAffected = persistentIdAttemptedToBeAffected;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerPermissionGroupChangeDeniedListeners, nameof(PermissionsPagesEventType.OnPlayerPermissionGroupChangeDenied));
            this.persistentIdAttemptedToBeAffected = 0u; // To prevent misuse of the API.
        }

        private void RaiseOnPermissionValueChangeDenied(PermissionGroup permissionGroupAttemptedToBeAffected, PermissionDefinition permissionAttemptedToBeAffected)
        {
            this.permissionGroupAttemptedToBeAffected = permissionGroupAttemptedToBeAffected;
            this.permissionAttemptedToBeAffected = permissionAttemptedToBeAffected;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPermissionValueChangeDeniedListeners, nameof(PermissionsPagesEventType.OnPermissionValueChangeDenied));
            this.permissionGroupAttemptedToBeAffected = null; // To prevent misuse of the API.
            this.permissionAttemptedToBeAffected = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
