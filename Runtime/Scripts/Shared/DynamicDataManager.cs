using UnityEngine;
using VRC.SDK3.Data;

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
        /// <summary>
        /// <para><see cref="string"/> dataName => <see cref="DynamicData"/> data</para>
        /// </summary>
        [System.NonSerialized] public DataDictionary globalDynamicDataByName = new DataDictionary();
        [System.NonSerialized] public DynamicData[] globalDynamicData = new DynamicData[ArrList.MinCapacity];
        [System.NonSerialized] public int globalDynamicDataCount = 0;
        #endregion

        private DynamicData[] overwriteUndoStack = new DynamicData[ArrList.MinCapacity];
        private int overwriteUndoStackCount = 0;

        [System.NonSerialized] public DynamicData dataForSerialization;

        protected virtual void Start()
        {
            dataForSerialization = (DynamicData)wannaBeClasses.NewDynamic(DynamicDataClassName);
        }

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

        public string GetFirstUnusedDataName(DataDictionary dataByName, string desiredName, bool alwaysUsePostfix)
        {
            if (!alwaysUsePostfix && !dataByName.ContainsKey(desiredName))
                return desiredName;
            int zero = 'A' - 1; // '@'
            int postfixNumber = 1; // Skip the zero, aka '@'.
            string result;
            do
            {
                int base27Postfix = postfixNumber++;
                string postfix = "";
                int currentMagnitude = 1;
                bool shouldSkip = true;
                do
                {
                    int digit = base27Postfix % 27;
                    if (shouldSkip && digit == 26) // Skip the next step in of the current magnitude as it would be a '@'.
                        postfixNumber += currentMagnitude;
                    else // Only skip consecutively. Z should skip to AA, ZA should not skip to A@A, ZZ should skip to AAA.
                        shouldSkip = false;
                    postfix = (char)(zero + digit) + postfix;
                    base27Postfix /= 27;
                    currentMagnitude *= 27;
                }
                while (base27Postfix != 0);
                result = desiredName + " " + postfix;
            }
            while (dataByName.ContainsKey(result));
            return result;
        }

        public void SendAddIA(DynamicData dataToSend)
        {
            dataToSend.id = 0u;
            lockstep.WriteCustomClass(dataToSend);
            lockstep.SendInputAction(addIAId);
        }

        [HideInInspector][SerializeField] private uint addIAId;
        [LockstepInputAction(nameof(addIAId))]
        public void OnAddIA()
        {
            DynamicData data = (DynamicData)lockstep.ReadCustomClass(DynamicDataClassName);
            data.id = nextId++;
            PerPlayerDynamicData p = GetPlayerData(data.owningPlayer); // A rare single letter variable name!
            if (data.isGlobal)
                OnAddIAInternal(globalAddPDef, globalDynamicDataByName, ref globalDynamicData, ref globalDynamicDataCount, data);
            else
                OnAddIAInternal(localAddPDef, p.localDynamicDataByName, ref p.localDynamicData, ref p.localDynamicDataCount, data);
        }

        private void OnAddIAInternal(
            PermissionDefinition addPDef,
            DataDictionary dataByName,
            ref DynamicData[] list,
            ref int count,
            DynamicData data)
        {
            if (!permissionManager.PlayerHasPermission(data.owningPlayer, addPDef))
                return;
            string dataName = data.dataName.Trim();
            if (dataName == "" || dataByName.ContainsKey(dataName))
                return;
            dataByName.Add(dataName, data);
            ArrList.Add(ref list, ref count, data);
            RaiseOnDataAdded(data);
        }

        protected abstract void RaiseOnDataAdded(DynamicData data);

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
            {
                DynamicData data = (DynamicData)lockstep.ReadCustomClass(DynamicDataClassName);
                globalDynamicDataByName.Add(data.dataName, data);
                globalDynamicData[i] = data;
            }
            return null;
        }
    }
}
