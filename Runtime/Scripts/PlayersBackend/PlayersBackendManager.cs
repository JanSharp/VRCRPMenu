using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(PlayersBackendEventAttribute), typeof(PlayersBackendEventType))]
    public class PlayersBackendManager : PlayersBackendManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;

        private int rpPlayerDataIndex;

        [PermissionDefinitionReference(nameof(editDisplayNamePermissionDef))]
        public string editDisplayNamePermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editDisplayNamePermissionDef;

        [PermissionDefinitionReference(nameof(editCharacterNamePermissionDef))]
        public string editCharacterNamePermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editCharacterNamePermissionDef;

        [PermissionDefinitionReference(nameof(deleteOfflinePlayerDataPermissionDef))]
        public string deleteOfflinePlayerDataPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition deleteOfflinePlayerDataPermissionDef;

        [PlayerDataEvent(PlayerDataEventType.OnRegisterCustomPlayerData)]
        public void OnRegisterCustomPlayerData()
        {
            playerDataManager.RegisterCustomPlayerData<RPPlayerData>(nameof(RPPlayerData));
        }

        [PlayerDataEvent(PlayerDataEventType.OnAllCustomPlayerDataRegistered)]
        public void OnAllCustomPlayerDataRegistered()
        {
            rpPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<RPPlayerData>(nameof(RPPlayerData));
        }

        private bool TryGetRPPlayerData(uint persistentId, out RPPlayerData rpPlayerData)
        {
            if (playerDataManager.TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData core))
            {
                rpPlayerData = (RPPlayerData)core.customPlayerData[rpPlayerDataIndex];
                return true;
            }
            rpPlayerData = null;
            return false;
        }

        public override void SendSetOverriddenDisplayNameIA(RPPlayerData rpPlayerData, string overriddenDisplayName)
        {
            if (overriddenDisplayName != null)
                overriddenDisplayName = overriddenDisplayName.Trim();
            lockstep.WriteSmallUInt(rpPlayerData.core.persistentId);
            lockstep.WriteString(overriddenDisplayName);
            lockstep.SendInputAction(setOverriddenDisplayNameIAId);
        }

        [HideInInspector][SerializeField] private uint setOverriddenDisplayNameIAId;
        [LockstepInputAction(nameof(setOverriddenDisplayNameIAId))]
        public void OnSetOverriddenDisplayNameIA()
        {
            uint persistentId = lockstep.ReadSmallUInt();
            string overriddenDisplayName = lockstep.ReadString();
            if (!TryGetRPPlayerData(persistentId, out RPPlayerData rpPlayerData))
                return;

            if (permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editDisplayNamePermissionDef))
                SetOverriddenDisplayNameInGS(rpPlayerData, overriddenDisplayName);
            else
                RaiseOnRPPlayerDataOverriddenDisplayNameChangeDenied(rpPlayerData);
        }

        public override void SetOverriddenDisplayNameInGS(RPPlayerData rpPlayerData, string overriddenDisplayName)
        {
            if (rpPlayerData.core.isDeleted) // Never the case when coming from OnSetOverriddenDisplayNameIA.
                return;
            if (overriddenDisplayName != null)
                overriddenDisplayName = overriddenDisplayName.Trim();
            string prev = rpPlayerData.overriddenDisplayName;
            if (prev == overriddenDisplayName)
                return;
            rpPlayerData.overriddenDisplayName = overriddenDisplayName;
            RaiseOnRPPlayerDataOverriddenDisplayNameChanged(rpPlayerData, prev);
        }

        public override void SendSetCharacterNameIA(RPPlayerData rpPlayerData, string characterName)
        {
            if (characterName != null) // Just reducing network data, not like it makes literally any difference.
                characterName = characterName.Trim();
            lockstep.WriteSmallUInt(rpPlayerData.core.persistentId);
            lockstep.WriteString(characterName);
            lockstep.SendInputAction(setCharacterNameIAId);
        }

        [HideInInspector][SerializeField] private uint setCharacterNameIAId;
        [LockstepInputAction(nameof(setCharacterNameIAId))]
        public void OnSetCharacterNameIA()
        {
            uint persistentId = lockstep.ReadSmallUInt();
            string characterName = lockstep.ReadString();
            if (!TryGetRPPlayerData(persistentId, out RPPlayerData rpPlayerData))
                return;

            if (permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editCharacterNamePermissionDef))
                SetCharacterNameInGS(rpPlayerData, characterName);
            else
                RaiseOnRPPlayerDataCharacterNameChangeDenied(rpPlayerData);
        }

        public override void SetCharacterNameInGS(RPPlayerData rpPlayerData, string characterName)
        {
            if (rpPlayerData.core.isDeleted) // Never the case when coming from OnSetCharacterNameIA.
                return;
            characterName = characterName == null ? "" : characterName.Trim();
            string prev = rpPlayerData.characterName;
            if (prev == characterName)
                return;
            rpPlayerData.characterName = characterName;
            RaiseOnRPPlayerDataCharacterNameChanged(rpPlayerData, prev);
        }

        public override void SendDeleteOfflinePlayerDataIA(CorePlayerData corePlayerData)
        {
            if (!corePlayerData.isOffline)
                return;
            lockstep.WriteSmallUInt(corePlayerData.persistentId);
            lockstep.SendInputAction(deleteOfflinePlayerDataIAId);
        }

        [HideInInspector][SerializeField] private uint deleteOfflinePlayerDataIAId;
        [LockstepInputAction(nameof(deleteOfflinePlayerDataIAId))]
        public void OnDeleteOfflinePlayerDataIA()
        {
            uint persistentId = lockstep.ReadSmallUInt();
            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, deleteOfflinePlayerDataPermissionDef))
            {
                RaiseOnDeleteOfflinePlayerDataDenied(persistentId);
                return;
            }
            if (!playerDataManager.TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData corePlayerData))
                return;
            playerDataManager.DeleteOfflinePlayerDataInGS(corePlayerData);
        }

        public override RPPlayerData SendingRPPlayerData => (RPPlayerData)playerDataManager.SendingPlayerData.customPlayerData[rpPlayerDataIndex];

        public override RPPlayerData GetRPPlayerData(CorePlayerData core) => (RPPlayerData)core.customPlayerData[rpPlayerDataIndex];

        public override void WriteRPPlayerDataRef(RPPlayerData rpPlayerData)
        {
            playerDataManager.WriteCorePlayerDataRef(rpPlayerData == null ? null : rpPlayerData.core);
        }

        public override RPPlayerData ReadRPPlayerDataRef()
        {
            CorePlayerData core = playerDataManager.ReadCorePlayerDataRef();
            return core == null ? null : (RPPlayerData)core.customPlayerData[rpPlayerDataIndex];
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPPlayerDataOverriddenDisplayNameChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPPlayerDataCharacterNameChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPPlayerDataOverriddenDisplayNameChangeDeniedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPPlayerDataCharacterNameChangeDeniedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onDeleteOfflinePlayerDataDeniedListeners;

        private RPPlayerData rpPlayerDataForEvent;
        public override RPPlayerData RPPlayerDataForEvent => rpPlayerDataForEvent;
        private string previousOverriddenDisplayName;
        public override string PreviousOverriddenDisplayName => previousOverriddenDisplayName;
        private string previousCharacterName;
        public override string PreviousCharacterName => previousCharacterName;

        private uint persistentIdAttemptedToBeAffected;
        public override uint PersistentIdAttemptedToBeAffected => persistentIdAttemptedToBeAffected;

        private void RaiseOnRPPlayerDataOverriddenDisplayNameChanged(RPPlayerData rpPlayerDataForEvent, string previousOverriddenDisplayName)
        {
            this.rpPlayerDataForEvent = rpPlayerDataForEvent;
            this.previousOverriddenDisplayName = previousOverriddenDisplayName;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPPlayerDataOverriddenDisplayNameChangedListeners, nameof(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged));
            this.rpPlayerDataForEvent = null; // To prevent misuse of the API.
            this.previousOverriddenDisplayName = null; // To prevent misuse of the API.
        }

        private void RaiseOnRPPlayerDataCharacterNameChanged(RPPlayerData rpPlayerDataForEvent, string previousCharacterName)
        {
            this.rpPlayerDataForEvent = rpPlayerDataForEvent;
            this.previousCharacterName = previousCharacterName;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPPlayerDataCharacterNameChangedListeners, nameof(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged));
            this.rpPlayerDataForEvent = null; // To prevent misuse of the API.
            this.previousCharacterName = null; // To prevent misuse of the API.
        }

        private void RaiseOnRPPlayerDataOverriddenDisplayNameChangeDenied(RPPlayerData rpPlayerDataForEvent)
        {
            this.rpPlayerDataForEvent = rpPlayerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPPlayerDataOverriddenDisplayNameChangeDeniedListeners, nameof(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChangeDenied));
            this.rpPlayerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnRPPlayerDataCharacterNameChangeDenied(RPPlayerData rpPlayerDataForEvent)
        {
            this.rpPlayerDataForEvent = rpPlayerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPPlayerDataCharacterNameChangeDeniedListeners, nameof(PlayersBackendEventType.OnRPPlayerDataCharacterNameChangeDenied));
            this.rpPlayerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnDeleteOfflinePlayerDataDenied(uint persistentIdAttemptedToBeAffected)
        {
            this.persistentIdAttemptedToBeAffected = persistentIdAttemptedToBeAffected;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onDeleteOfflinePlayerDataDeniedListeners, nameof(PlayersBackendEventType.OnDeleteOfflinePlayerDataDenied));
            this.persistentIdAttemptedToBeAffected = 0u; // To prevent misuse of the API.
        }

        #endregion
    }
}
