using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [LockstepGameStateDependency(typeof(PlayerDataManagerAPI), SelfLoadsBeforeDependency = true)]
    [CustomRaisedEventsDispatcher(typeof(PlayersBackendEventAttribute), typeof(PlayersBackendEventType))]
    public class PlayersBackendManager : PlayersBackendManagerAPI
    {
        public override string GameStateInternalName => "jansharp.rp-menu-players-backend";
        public override string GameStateDisplayName => "RP Menu Players Backend";
        public override bool GameStateSupportsImportExport => true;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;
        [SerializeField] private PlayersBackendImportExportOptionsUI exportUI;
        [SerializeField] private PlayersBackendImportExportOptionsUI importUI;
        public override LockstepGameStateOptionsUI ExportUI => exportUI;
        public override LockstepGameStateOptionsUI ImportUI => importUI;

        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;

        public override PlayersBackendImportExportOptions ExportOptions => (PlayersBackendImportExportOptions)OptionsForCurrentExport;
        public override PlayersBackendImportExportOptions ImportOptions => (PlayersBackendImportExportOptions)OptionsForCurrentImport;
        private PlayersBackendImportExportOptions optionsFromExport;
        public override PlayersBackendImportExportOptions OptionsFromExport => optionsFromExport;

        private int rpPlayerDataIndex;

        [PermissionDefinitionReference(nameof(editDisplayNamePDef))]
        public string editDisplayNamePermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editDisplayNamePDef;

        [PermissionDefinitionReference(nameof(editCharacterNamePDef))]
        public string editCharacterNamePermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editCharacterNamePDef;

        [PermissionDefinitionReference(nameof(deleteOfflinePlayerDataPDef))]
        public string deleteOfflinePlayerDataPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition deleteOfflinePlayerDataPDef;

        [PermissionDefinitionReference(nameof(editPermissionsPDef))]
        public string editPermissionsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition editPermissionsPDef;

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

        public override void SendSetOverriddenDisplayNameIA(RPPlayerData rpPlayerData, string overriddenDisplayName)
        {
            if (overriddenDisplayName != null)
                overriddenDisplayName = overriddenDisplayName.Trim();
            WriteRPPlayerDataRef(rpPlayerData);
            lockstep.WriteString(overriddenDisplayName);
            lockstep.SendInputAction(setOverriddenDisplayNameIAId);
        }

        [HideInInspector][SerializeField] private uint setOverriddenDisplayNameIAId;
        [LockstepInputAction(nameof(setOverriddenDisplayNameIAId))]
        public void OnSetOverriddenDisplayNameIA()
        {
            RPPlayerData rpPlayerData = ReadRPPlayerDataRef();
            string overriddenDisplayName = lockstep.ReadString();
            if (rpPlayerData == null)
                return;

            if (permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editDisplayNamePDef))
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
            WriteRPPlayerDataRef(rpPlayerData);
            lockstep.WriteString(characterName);
            lockstep.SendInputAction(setCharacterNameIAId);
        }

        [HideInInspector][SerializeField] private uint setCharacterNameIAId;
        [LockstepInputAction(nameof(setCharacterNameIAId))]
        public void OnSetCharacterNameIA()
        {
            RPPlayerData rpPlayerData = ReadRPPlayerDataRef();
            string characterName = lockstep.ReadString();
            if (rpPlayerData == null)
                return;

            if (permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, editCharacterNamePDef))
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
            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, deleteOfflinePlayerDataPDef))
            {
                RaiseOnDeleteOfflinePlayerDataDenied(persistentId, isLastPlayerWhoCanEditPermissions: false);
                return;
            }
            if (!playerDataManager.TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData corePlayerData))
                return;

            if (AnyOtherPlayerHasEditPermissions(corePlayerData))
                playerDataManager.DeleteOfflinePlayerDataInGS(corePlayerData);
            else
                RaiseOnDeleteOfflinePlayerDataDenied(persistentId, isLastPlayerWhoCanEditPermissions: true);
        }

        private bool AnyOtherPlayerHasEditPermissions(CorePlayerData playerBeingDeleted)
        {
            PermissionGroup group = permissionManager.GetPermissionsPlayerData(playerBeingDeleted).permissionGroup;
            if (group.playersInGroupCount != 1 || !group.permissionValues[editPermissionsPDef.index])
                return true;
            PermissionGroup[] groups = permissionManager.PermissionGroupsRaw;
            int count = permissionManager.PermissionGroupsCount;
            for (int i = 0; i < count; i++)
            {
                PermissionGroup otherGroup = groups[i];
                if (otherGroup != group && otherGroup.playersInGroupCount != 0 && otherGroup.permissionValues[editPermissionsPDef.index])
                    return true;
            }
            return false;
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

        #region Serialization

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
            if (!isExport)
                return;
            lockstep.WriteCustomClass(exportOptions);
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
            if (!isImport)
                return null;
            optionsFromExport = (PlayersBackendImportExportOptions)lockstep.ReadCustomClass(nameof(PlayersBackendImportExportOptions));
            return null;
        }

        [LockstepEvent(LockstepEventType.OnImportFinished, Order = 1000)]
        public void OnImportFinished()
        {
            optionsFromExport = null;
        }

        #endregion

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
        private bool isLastPlayerWhoCanEditPermissions;
        public override bool IsLastPlayerWhoCanEditPermissions => isLastPlayerWhoCanEditPermissions;

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

        private void RaiseOnDeleteOfflinePlayerDataDenied(uint persistentIdAttemptedToBeAffected, bool isLastPlayerWhoCanEditPermissions)
        {
            this.persistentIdAttemptedToBeAffected = persistentIdAttemptedToBeAffected;
            this.isLastPlayerWhoCanEditPermissions = isLastPlayerWhoCanEditPermissions;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onDeleteOfflinePlayerDataDeniedListeners, nameof(PlayersBackendEventType.OnDeleteOfflinePlayerDataDenied));
            this.persistentIdAttemptedToBeAffected = 0u; // To prevent misuse of the API.
            this.isLastPlayerWhoCanEditPermissions = false; // To prevent misuse of the API.
        }

        #endregion
    }
}
