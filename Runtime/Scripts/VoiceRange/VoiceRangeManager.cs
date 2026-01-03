using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [LockstepGameStateDependency(typeof(PlayerDataManagerAPI))]
    public class VoiceRangeManager : VoiceRangeManagerAPI
    {
        public override string GameStateInternalName => "jansharp.voice-range-manager";
        public override string GameStateDisplayName => "Voice Range Manager";
        public override bool GameStateSupportsImportExport => true;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;
        public override LockstepGameStateOptionsUI ExportUI => null;
        public override LockstepGameStateOptionsUI ImportUI => null;

        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        #region GameState

        [Tooltip("Maximum 32 definitions.")]
        [SerializeField] private VoiceRangeDefinition[] rangeDefs;
        [SerializeField] public VoiceRangeDefinition defaultRangeDef;
        private DataDictionary rangeDefsByInternalName = new DataDictionary();
        private int rangeDefsCount;

        private int voiceRangePlayerDataIndex;

        private int defaultVoiceRangeIndex;
        private uint defaultShowInWorldMask;
        private uint defaultShowInHUDMask;
        public override int DefaultVoiceRangeIndex => defaultVoiceRangeIndex;
        public override uint DefaultShowInWorldMask => defaultShowInWorldMask;
        public override uint DefaultShowInHUDMask => defaultShowInHUDMask;

        #endregion

        private uint[] fromImportedFlag;
        private uint[] toLiveFlag;
        private int importToRemapCount;
        private uint importKeepMask;
        private uint importAddWorldMask;
        private uint importAddHUDMask;

        private int suspendedIndexInArray = 0;
        private System.Diagnostics.Stopwatch suspensionSw = new System.Diagnostics.Stopwatch();
        private const long MaxWorkMSPerFrame = 10L;

        private bool LogicIsRunningLong()
        {
            if (suspensionSw.ElapsedMilliseconds >= MaxWorkMSPerFrame)
            {
                lockstep.FlagToContinueNextFrame();
                return true;
            }
            return false;
        }

        private void Start()
        {
            rangeDefsCount = rangeDefs.Length;
            if (rangeDefsCount > 32)
            {
                Debug.LogError($"[RPMenu] The maximum amount of voice ranges is 32, got {rangeDefsCount}.", this);
                return;
            }
            if (defaultRangeDef == null)
            {
                Debug.LogError($"[RPMenu] The default Voice Range Definition must not be null.", this);
                return;
            }
            bool foundDefault = false;
            for (int i = 0; i < rangeDefsCount; i++)
            {
                VoiceRangeDefinition def = rangeDefs[i];
                if (def == null)
                {
                    Debug.LogError($"[RPMenu] The Voice Range Definitions array must not contains any nulls, "
                        + $"got null at index {i}.", this);
                    return;
                }
                if (rangeDefsByInternalName.ContainsKey(def.internalName))
                {
                    Debug.LogError($"[RPMenu] There are multiple Voice Range Definitions trying to use the "
                        + $"same internal name '{def.internalName}'.", this);
                    return;
                }
                def.index = i;
                def.bitMaskFlag = 1u << i;
                rangeDefsByInternalName.Add(def.internalName, def);
                if (def.showInWorldByDefault)
                    defaultShowInWorldMask |= def.bitMaskFlag;
                if (def.showInHUDByDefault)
                    defaultShowInHUDMask |= def.bitMaskFlag;
                if (def == defaultRangeDef)
                    foundDefault = true;
            }
            if (!foundDefault)
            {
                Debug.LogError($"[RPMenu] The default Voice Range Definition must also be in the list of all "
                    + "Voice Range Definitions.", this);
                return;
            }
            defaultVoiceRangeIndex = defaultRangeDef.index;
        }

        [PlayerDataEvent(PlayerDataEventType.OnRegisterCustomPlayerData)]
        public void OnRegisterCustomPlayerData()
        {
            playerDataManager.RegisterCustomPlayerData<VoiceRangePlayerData>(nameof(VoiceRangePlayerData));
        }

        [PlayerDataEvent(PlayerDataEventType.OnAllCustomPlayerDataRegistered)]
        public void OnAllCustomPlayerDataRegistered()
        {
            voiceRangePlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<VoiceRangePlayerData>(nameof(VoiceRangePlayerData));
        }

        #region Serialization

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
            if (!isExport)
                return;
            lockstep.WriteSmallUInt((uint)rangeDefsCount);
            for (int i = 0; i < rangeDefsCount; i++)
                lockstep.WriteString(rangeDefs[i].internalName);
        }

        private bool BuildImportRemapAndMasks()
        {
            uint importedDefsCount = lockstep.ReadSmallUInt();
            DataDictionary importedInternalNamesLut = new DataDictionary();
            fromImportedFlag = new uint[importedDefsCount];
            toLiveFlag = new uint[importedDefsCount];
            importToRemapCount = 0;
            importKeepMask = 0u;
            for (int i = 0; i < importedDefsCount; i++)
            {
                string importedInternalName = lockstep.ReadString();
                importedInternalNamesLut.Add(importedInternalName, true);
                if (!rangeDefsByInternalName.TryGetValue(importedInternalName, out DataToken defToken))
                    continue;
                VoiceRangeDefinition def = (VoiceRangeDefinition)defToken.Reference;
                if (def.index == i)
                {
                    importKeepMask |= def.bitMaskFlag;
                    continue;
                }
                fromImportedFlag[importToRemapCount] = 1u << i;
                toLiveFlag[importToRemapCount] = def.bitMaskFlag;
                importToRemapCount++;
            }
            importAddWorldMask = 0u;
            importAddHUDMask = 0u;
            for (int i = 0; i < rangeDefsCount; i++)
            {
                VoiceRangeDefinition def = rangeDefs[i];
                if (importedInternalNamesLut.ContainsKey(def.internalName))
                    continue;
                if (def.showInWorldByDefault)
                    importAddWorldMask |= def.bitMaskFlag;
                if (def.showInHUDByDefault)
                    importAddHUDMask |= def.bitMaskFlag;
            }
            return importToRemapCount != 0
                || importAddWorldMask != 0u
                || importAddHUDMask != 0u
                || importedDefsCount > rangeDefsCount;
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
            if (!isImport)
                return null;
            suspensionSw.Restart();
            if (!lockstep.IsContinuationFromPrevFrame && !BuildImportRemapAndMasks())
                return null;
            int count = playerDataManager.AllCorePlayerDataCount;
            CorePlayerData[] players = playerDataManager.AllCorePlayerDataRaw;
            while (suspendedIndexInArray < count)
            {
                if (LogicIsRunningLong())
                    return null;
                VoiceRangePlayerData player = (VoiceRangePlayerData)players[suspendedIndexInArray].customPlayerData[voiceRangePlayerDataIndex];
                uint worldMask = player.showInWorldMask;
                uint hudMask = player.showInHUDMask;
                uint newWorldMask = (worldMask & importKeepMask) | importAddWorldMask;
                uint newHUDMask = (hudMask & importKeepMask) | importAddHUDMask;
                for (int i = 0; i < importToRemapCount; i++)
                {
                    uint fromFlag = fromImportedFlag[i];
                    uint toFlag = toLiveFlag[i];
                    if ((worldMask & fromFlag) != 0u)
                        newWorldMask |= toFlag;
                    if ((hudMask & fromFlag) != 0u)
                        newHUDMask |= toFlag;
                }
                player.showInWorldMask = newWorldMask;
                player.showInHUDMask = newHUDMask;
                player.latencyShowInWorldMask = newWorldMask;
                player.latencyShowInHUDMask = newHUDMask;
                suspendedIndexInArray++;
            }
            suspendedIndexInArray = 0;
            return null;
        }

        #endregion
    }
}
