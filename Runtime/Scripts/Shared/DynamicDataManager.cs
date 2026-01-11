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
        /// <para><see cref="uint"/> id => <see cref="DynamicData"/> data</para>
        /// <para>Has global and all local data. Literally all data part of the game state.</para>
        /// </summary>
        [System.NonSerialized] public DataDictionary allDynamicDataById = new DataDictionary();
        /// <summary>
        /// <para><see cref="string"/> dataName => <see cref="DynamicData"/> data</para>
        /// </summary>
        [System.NonSerialized] public DataDictionary globalDynamicDataByName = new DataDictionary();
        [System.NonSerialized] public DynamicData[] globalDynamicData = new DynamicData[WannaBeArrList.MinCapacity];
        [System.NonSerialized] public int globalDynamicDataCount = 0;
        #endregion

        /// <summary>
        /// <para>Holds strong references.</para>
        /// </summary>
        private DynamicData[] overwriteUndoStack = new DynamicData[WannaBeArrList.MinCapacity];
        private int overwriteUndoStackCount = 0;
        public int OverwriteUndoStackSize => overwriteUndoStackCount;
        private const float UndoStackTimeoutSeconds = 60f;
        /// <summary>
        /// <para>Holds weak references.</para>
        /// </summary>
        private DynamicData[] overwriteUndoTimeoutQueue = new DynamicData[ArrQueue.MinCapacity];
        // cSpell:ignore outq
        private int outqStartIndex = 0;
        private int outqCount = 0;

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
            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, addPDef))
                return;
            string dataName = data.dataName.Trim();
            if (dataName == "" || dataByName.ContainsKey(dataName))
                return;
            allDynamicDataById.Add(data.id, data);
            dataByName.Add(dataName, data);
            // Already is a strong reference, do not increment refs counter when adding, do not use WannaBeArrList.
            ArrList.Add(ref list, ref count, data);
            RaiseOnDataAdded(data);
        }

        public void SendOverwriteIA(DynamicData dataToSend, bool isUndo)
        {
            dataToSend.id = 0u;
            lockstep.WriteCustomClass(dataToSend);
            lockstep.WriteFlags(isUndo);
            lockstep.SendInputAction(overwriteIAId);
        }

        [HideInInspector][SerializeField] private uint overwriteIAId;
        [LockstepInputAction(nameof(overwriteIAId))]
        public void OnOverwriteIA()
        {
            DynamicData data = (DynamicData)lockstep.ReadCustomClass(DynamicDataClassName);
            lockstep.ReadFlags(out bool isUndo);
            data.id = nextId++;
            PerPlayerDynamicData p = GetPlayerData(data.owningPlayer); // A rare single letter variable name!
            if (data.isGlobal)
                OnOverwriteIAInternal(globalAddPDef, globalDynamicDataByName, ref globalDynamicData, ref globalDynamicDataCount, data, isUndo);
            else
                OnOverwriteIAInternal(localAddPDef, p.localDynamicDataByName, ref p.localDynamicData, ref p.localDynamicDataCount, data, isUndo);
        }

        private void OnOverwriteIAInternal(
            PermissionDefinition overwritePDef,
            DataDictionary dataByName,
            ref DynamicData[] list,
            ref int count,
            DynamicData data,
            bool isUndo)
        {
            CorePlayerData sendingPlayerData = playerDataManager.SendingPlayerData;
            if (!permissionManager.PlayerHasPermission(sendingPlayerData, overwritePDef))
                return;
            string dataName = data.dataName;
            if (!dataByName.TryGetValue(dataName, out DataToken toOverwriteToken))
                return;
            DynamicData toOverwrite = (DynamicData)toOverwriteToken.Reference;
            int index = ArrList.IndexOf(ref list, ref count, toOverwrite);
            list[index] = data;
            allDynamicDataById.Remove(toOverwrite.id);
            allDynamicDataById.Add(data.id, data);
            dataByName[dataName] = data;
            if (!isUndo && sendingPlayerData.isLocal)
            {
                // Already is a strong reference, do not increment refs counter when adding, do not use WannaBeArrList.
                ArrList.Add(ref overwriteUndoStack, ref overwriteUndoStackCount, toOverwrite);
                ArrQueue.Enqueue(ref overwriteUndoTimeoutQueue, ref outqStartIndex, ref outqCount, toOverwrite);
                SendCustomEventDelayedSeconds(nameof(OnOverwriteUndoTimedOut), UndoStackTimeoutSeconds);
                RaiseOnOverwriteUndoStackChanged();
            }
            RaiseOnDataOverwritten(data, toOverwrite);
        }

        public void PopFromOverwriteUndoStack()
        {
            WannaBeArrList.RemoveAt(ref overwriteUndoStack, ref overwriteUndoStackCount, overwriteUndoStackCount - 1);
            RaiseOnOverwriteUndoStackChanged();
        }

        public DynamicData GetTopFromOverwriteUndoStack()
        {
            return overwriteUndoStackCount == 0 ? null : overwriteUndoStack[overwriteUndoStackCount - 1];
        }

        public void OnOverwriteUndoTimedOut()
        {
            DynamicData timedOut = ArrQueue.Dequeue(ref overwriteUndoTimeoutQueue, ref outqStartIndex, ref outqCount);
            if (timedOut == null) // Weak references, could be deleted already.
                return;
            if (WannaBeArrList.Remove(ref overwriteUndoStack, ref overwriteUndoStackCount, timedOut) == -1)
                return;
            RaiseOnOverwriteUndoStackChanged();
        }

        public void SendDeleteIA(DynamicData data)
        {
            lockstep.WriteSmallUInt(data.id);
            lockstep.SendInputAction(deleteIAId);
        }

        [HideInInspector][SerializeField] private uint deleteIAId;
        [LockstepInputAction(nameof(deleteIAId))]
        public void OnDeleteIA()
        {
            uint id = lockstep.ReadSmallUInt();
            if (!allDynamicDataById.TryGetValue(id, out DataToken dataToken))
                return;
            DynamicData data = (DynamicData)dataToken.Reference;
            PerPlayerDynamicData p = GetPlayerData(data.owningPlayer); // A rare single letter variable name!
            if (data.isGlobal)
                OnDeleteIAInternal(globalDeletePDef, globalDynamicDataByName, ref globalDynamicData, ref globalDynamicDataCount, data);
            else
                OnDeleteIAInternal(localDeletePDef, p.localDynamicDataByName, ref p.localDynamicData, ref p.localDynamicDataCount, data);
        }

        private void OnDeleteIAInternal(
            PermissionDefinition deletePDef,
            DataDictionary dataByName,
            ref DynamicData[] list,
            ref int count,
            DynamicData data)
        {
            CorePlayerData sendingPlayerData = playerDataManager.SendingPlayerData;
            if (!permissionManager.PlayerHasPermission(sendingPlayerData, deletePDef))
                return;
            allDynamicDataById.Remove(data.id);
            dataByName.Remove(data.dataName);
            ArrList.Remove(ref list, ref count, data);
            RaiseOnDataDeleted(data);
            data.DecrementRefsCount();
        }

        protected abstract void RaiseOnDataAdded(DynamicData data);

        protected abstract void RaiseOnDataOverwritten(DynamicData data, DynamicData overwrittenData);

        protected abstract void RaiseOnDataDeleted(DynamicData data);

        protected abstract void RaiseOnOverwriteUndoStackChanged();

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
                allDynamicDataById.Add(data.id, data);
                globalDynamicDataByName.Add(data.dataName, data);
                globalDynamicData[i] = data;
            }
            return null;
        }
    }
}
