using UnityEngine;

namespace JanSharp
{
    public abstract class DynamicDataManager : LockstepGameState
    {
        public override bool GameStateSupportsImportExport => false;
        public override LockstepGameStateOptionsUI ExportUI => null;
        public override LockstepGameStateOptionsUI ImportUI => null;

        public abstract string DynamicDataClassName { get; }
        public abstract string PerPlayerDataClassName { get; }

        [HideInInspector][SerializeField][SingletonReference] protected WannaBeClassesManager wannaBeClasses;
        [HideInInspector][SerializeField][SingletonReference] protected PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] protected PermissionManagerAPI permissionManager;

        protected int playerDataIndex;

        #region LocalPermissions

        [PermissionDefinitionReference(nameof(localAddPDef))]
        public string localAddPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localAddPDef;

        [PermissionDefinitionReference(nameof(localOverwritePDef))]
        public string localOverwritePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localOverwritePDef;

        [PermissionDefinitionReference(nameof(localLoadPDef))]
        public string localLoadPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localLoadPDef;

        [PermissionDefinitionReference(nameof(localDeletePDef))]
        public string localDeletePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localDeletePDef;

        #endregion

        #region GlobalPermissions

        [PermissionDefinitionReference(nameof(globalAddPDef))]
        public string globalAddPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalAddPDef;

        [PermissionDefinitionReference(nameof(globalOverwritePDef))]
        public string globalOverwritePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalOverwritePDef;

        [PermissionDefinitionReference(nameof(globalLoadPDef))]
        public string globalLoadPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalLoadPDef;

        [PermissionDefinitionReference(nameof(globalDeletePDef))]
        public string globalDeletePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalDeletePDef;

        #endregion

        #region GameState
        /// <summary>
        /// <para><c>0u</c> is invalid.</para>
        /// </summary>
        private uint nextId = 1u;
        private DynamicData[] globalDynamicData = new DynamicData[ArrList.MinCapacity];
        private int globalDynamicDataCount = 0;
        #endregion

        private DynamicData[] overwriteUndoStack = new DynamicData[ArrList.MinCapacity];
        private int overwriteUndoStackCount = 0;

        [PlayerDataEvent(PlayerDataEventType.OnRegisterCustomPlayerData)]
        public void OnRegisterCustomPlayerData()
        {
            playerDataManager.RegisterCustomPlayerDataDynamic(PerPlayerDataClassName);
        }

        [PlayerDataEvent(PlayerDataEventType.OnAllCustomPlayerDataRegistered)]
        public void OnAllCustomPlayerDataRegistered()
        {
            playerDataIndex = playerDataManager.GetPlayerDataClassNameIndexDynamic(PerPlayerDataClassName);
        }

        public PerPlayerDynamicData SendingPlayerData => (PerPlayerDynamicData)playerDataManager.SendingPlayerData.customPlayerData[playerDataIndex];

        public PerPlayerDynamicData GetPlayerData(CorePlayerData core) => (PerPlayerDynamicData)core.customPlayerData[playerDataIndex];

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
            lockstep.WriteSmallUInt(nextId);
            lockstep.WriteSmallUInt((uint)globalDynamicDataCount);
            for (int i = 0; i < globalDynamicDataCount; i++)
                lockstep.WriteCustomClass(globalDynamicData[i]);
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
            nextId = lockstep.ReadSmallUInt();
            globalDynamicDataCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref globalDynamicData, globalDynamicDataCount);
            for (int i = 0; i < globalDynamicDataCount; i++)
                globalDynamicData[i] = (DynamicData)lockstep.ReadCustomClass(DynamicDataClassName);
            return null;
        }
    }
}
