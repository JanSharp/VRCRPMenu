using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [LockstepGameStateDependency(typeof(PlayerDataManagerAPI))]
    [CustomRaisedEventsDispatcher(typeof(VoiceRangeEventAttribute), typeof(VoiceRangeEventType))]
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
        [SerializeField] private VoiceRangeDefinition defaultRangeDef;
        private DataDictionary rangeDefsByInternalName = new DataDictionary();
        private int rangeDefsCount;
        public override int VoiceRangeDefinitionCount => rangeDefsCount;
        public override VoiceRangeDefinition GetVoiceRangeDefinition(int index) => rangeDefs[index];
        public override VoiceRangeDefinition GetVoiceRangeDefinition(string internalName) => (VoiceRangeDefinition)rangeDefsByInternalName[internalName].Reference;

        private int voiceRangePlayerDataIndex;
        private uint localPlayerId;
        private VoiceRangePlayerData localPlayer;
        public override VoiceRangePlayerData LocalPlayer => localPlayer;

        private int defaultVoiceRangeIndex;
        private uint defaultShowInWorldMask;
        [SerializeField] private VoiceRangeVisualizationType defaultWorldVisualType;
        private uint defaultShowInHUDMask;
        [SerializeField] private VoiceRangeVisualizationType defaultHUDVisualType;
        public override int DefaultVoiceRangeIndex => defaultVoiceRangeIndex;
        public override uint DefaultShowInWorldMask => defaultShowInWorldMask;
        public override VoiceRangeVisualizationType DefaultWorldVisualType => defaultWorldVisualType;
        public override uint DefaultShowInHUDMask => defaultShowInHUDMask;
        public override VoiceRangeVisualizationType DefaultHUDVisualType => defaultHUDVisualType;

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

        #region Initialization

        private void Start()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
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
            if (defaultRangeDef.permissionDef != null)
            {
                Debug.LogError($"[RPMenu] The default Voice Range Definition must not have an associated permission.", this);
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

        [PlayerDataEvent(PlayerDataEventType.OnLocalPlayerDataAvailable)]
        public void OnLocalPlayerDataAvailable()
        {
            localPlayer = (VoiceRangePlayerData)playerDataManager.GetCorePlayerDataForPlayerId(localPlayerId).customPlayerData[voiceRangePlayerDataIndex];
        }

        [LockstepEvent(LockstepEventType.OnPostClientBeginCatchUp)]
        public void OnPostClientBeginCatchUp()
        {
            // When a player leaves the world their voice range mode gets set to default, thus the only time
            // this logic here would be required is if some system changed a players mode while they were
            // offline and the permission group they are in also does not have the permission for that voice
            // range mode.
            for (int i = 0; i < rangeDefsCount; i++)
                rangeDefs[i].Resolve();
        }

        #endregion

        #region InputActions

        public override void SendSetVoiceRangeIndexIA(VoiceRangePlayerData player, int voiceRangeIndex)
        {
            if (!lockstep.IsInitialized)
                return;
            WriteVoiceRangePlayerDataRef(player);
            lockstep.WriteSmallUInt((uint)voiceRangeIndex);
            player.latencyHiddenUniqueIds.Add(lockstep.SendInputAction(setVoiceRangeIndexIAId), true);
            player.latencyVoiceRangeIndex = voiceRangeIndex;
            RaiseOnVoiceRangeIndexChangedInLatency(player);
        }

        [HideInInspector][SerializeField] private uint setVoiceRangeIndexIAId;
        [LockstepInputAction(nameof(setVoiceRangeIndexIAId))]
        public void OnSetVoiceRangeIndexIA()
        {
            VoiceRangePlayerData player = ReadVoiceRangePlayerDataRef();
            if (player == null)
                return;
            int voiceRangeIndex = (int)lockstep.ReadSmallUInt();
            VoiceRangeDefinition def = rangeDefs[voiceRangeIndex];
            if (!def.PlayerHasPermission(player.core))
            {
                if (player.latencyHiddenUniqueIds.Count == 0)
                    return;
                player.latencyHiddenUniqueIds.Clear(); // Latency state has predicted incorrectly.
                player.latencyVoiceRangeIndex = player.voiceRangeIndex;
                RaiseOnVoiceRangeIndexChangedInLatency(player);
                return;
            }
            player.voiceRangeIndex = voiceRangeIndex;
            if (player.latencyHiddenUniqueIds.Remove(lockstep.SendingUniqueId))
            {
                RaiseOnVoiceRangeIndexChanged(player);
                return;
            }
            player.latencyHiddenUniqueIds.Clear(); // Latency state may have predicted incorrectly.
            player.latencyVoiceRangeIndex = voiceRangeIndex;
            RaiseOnVoiceRangeIndexChangedInLatency(player);
            RaiseOnVoiceRangeIndexChanged(player);
        }

        public override void SendSetInWorldSettingsIA(VoiceRangePlayerData player, uint showMask, VoiceRangeVisualizationType visualType)
        {
            if (!lockstep.IsInitialized)
                return;
            WriteVoiceRangePlayerDataRef(player);
            lockstep.WriteSmallUInt(showMask);
            lockstep.WriteByte((byte)visualType);
            player.latencyHiddenUniqueIds.Add(lockstep.SendInputAction(setInWorldSettingsIAId), true);
            player.latencyShowInWorldMask = showMask;
            player.latencyWorldVisualType = visualType;
            RaiseOnInWorldSettingsChangedInLatency(player);
        }

        [HideInInspector][SerializeField] private uint setInWorldSettingsIAId;
        [LockstepInputAction(nameof(setInWorldSettingsIAId))]
        public void OnSetInWorldSettingsIA()
        {
            VoiceRangePlayerData player = ReadVoiceRangePlayerDataRef();
            if (player == null)
                return;
            player.showInWorldMask = lockstep.ReadSmallUInt();
            player.worldVisualType = (VoiceRangeVisualizationType)lockstep.ReadByte();
            if (player.latencyHiddenUniqueIds.Remove(lockstep.SendingUniqueId))
            {
                RaiseOnInWorldSettingsChanged(player);
                return;
            }
            player.latencyHiddenUniqueIds.Clear(); // Latency state may have predicted incorrectly.
            player.latencyShowInWorldMask = player.showInWorldMask;
            player.latencyWorldVisualType = player.worldVisualType;
            RaiseOnInWorldSettingsChangedInLatency(player);
            RaiseOnInWorldSettingsChanged(player);
        }

        public override void SendSetInHUDSettingsIA(VoiceRangePlayerData player, uint showMask, VoiceRangeVisualizationType visualType)
        {
            if (!lockstep.IsInitialized)
                return;
            WriteVoiceRangePlayerDataRef(player);
            lockstep.WriteSmallUInt(showMask);
            lockstep.WriteByte((byte)visualType);
            player.latencyHiddenUniqueIds.Add(lockstep.SendInputAction(setInHUDSettingsIAId), true);
            player.latencyShowInHUDMask = showMask;
            player.latencyHUDVisualType = visualType;
            RaiseOnInHUDSettingsChangedInLatency(player);
        }

        [HideInInspector][SerializeField] private uint setInHUDSettingsIAId;
        [LockstepInputAction(nameof(setInHUDSettingsIAId))]
        public void OnSetInHUDSettingsIA()
        {
            VoiceRangePlayerData player = ReadVoiceRangePlayerDataRef();
            if (player == null)
                return;
            player.showInHUDMask = lockstep.ReadSmallUInt();
            player.hudVisualType = (VoiceRangeVisualizationType)lockstep.ReadByte();
            if (player.latencyHiddenUniqueIds.Remove(lockstep.SendingUniqueId))
            {
                RaiseOnInHUDSettingsChanged(player);
                return;
            }
            player.latencyHiddenUniqueIds.Clear(); // Latency state may have predicted incorrectly.
            player.latencyShowInHUDMask = player.showInHUDMask;
            player.latencyHUDVisualType = player.hudVisualType;
            RaiseOnInHUDSettingsChangedInLatency(player);
            RaiseOnInHUDSettingsChanged(player);
        }

        #endregion

        #region Utils

        public override VoiceRangePlayerData GetVoiceRangePlayerData(CorePlayerData core) => (VoiceRangePlayerData)core.customPlayerData[voiceRangePlayerDataIndex];

        public override void WriteVoiceRangePlayerDataRef(VoiceRangePlayerData voiceRangePlayerData)
        {
            playerDataManager.WriteCorePlayerDataRef(voiceRangePlayerData == null ? null : voiceRangePlayerData.core);
        }

        public override VoiceRangePlayerData ReadVoiceRangePlayerDataRef()
        {
            CorePlayerData core = playerDataManager.ReadCorePlayerDataRef();
            return core == null ? null : (VoiceRangePlayerData)core.customPlayerData[voiceRangePlayerDataIndex];
        }

        #endregion

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

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onVoiceRangeIndexChangedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onVoiceRangeIndexChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalVoiceRangeIndexChangedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalVoiceRangeIndexChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onInWorldSettingsChangedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onInWorldSettingsChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalInWorldSettingsChangedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalInWorldSettingsChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onInHUDSettingsChangedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onInHUDSettingsChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalInHUDSettingsChangedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalInHUDSettingsChangedListeners;

        private VoiceRangePlayerData playerDataForEvent;
        public override VoiceRangePlayerData PlayerDataForEvent => playerDataForEvent;

        private void RaiseOnVoiceRangeIndexChangedInLatency(VoiceRangePlayerData playerDataForEvent)
        {
            this.playerDataForEvent = playerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onVoiceRangeIndexChangedInLatencyListeners, nameof(VoiceRangeEventType.OnVoiceRangeIndexChangedInLatency));
            this.playerDataForEvent = null; // To prevent misuse of the API.
            if (playerDataForEvent.core.isLocal)
                RaiseOnLocalVoiceRangeIndexChangedInLatency();
        }

        private void RaiseOnVoiceRangeIndexChanged(VoiceRangePlayerData playerDataForEvent)
        {
            this.playerDataForEvent = playerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onVoiceRangeIndexChangedListeners, nameof(VoiceRangeEventType.OnVoiceRangeIndexChanged));
            this.playerDataForEvent = null; // To prevent misuse of the API.
            if (playerDataForEvent.core.isLocal)
                RaiseOnLocalVoiceRangeIndexChanged();
        }

        private void RaiseOnLocalVoiceRangeIndexChangedInLatency()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalVoiceRangeIndexChangedInLatencyListeners, nameof(VoiceRangeEventType.OnLocalVoiceRangeIndexChangedInLatency));
        }

        private void RaiseOnLocalVoiceRangeIndexChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalVoiceRangeIndexChangedListeners, nameof(VoiceRangeEventType.OnLocalVoiceRangeIndexChanged));
        }

        private void RaiseOnInWorldSettingsChangedInLatency(VoiceRangePlayerData playerDataForEvent)
        {
            this.playerDataForEvent = playerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onInWorldSettingsChangedInLatencyListeners, nameof(VoiceRangeEventType.OnInWorldSettingsChangedInLatency));
            this.playerDataForEvent = null; // To prevent misuse of the API.
            if (playerDataForEvent.core.isLocal)
                RaiseOnLocalInWorldSettingsChangedInLatency();
        }

        private void RaiseOnInWorldSettingsChanged(VoiceRangePlayerData playerDataForEvent)
        {
            this.playerDataForEvent = playerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onInWorldSettingsChangedListeners, nameof(VoiceRangeEventType.OnInWorldSettingsChanged));
            this.playerDataForEvent = null; // To prevent misuse of the API.
            if (playerDataForEvent.core.isLocal)
                RaiseOnLocalInWorldSettingsChanged();
        }

        private void RaiseOnLocalInWorldSettingsChangedInLatency()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalInWorldSettingsChangedInLatencyListeners, nameof(VoiceRangeEventType.OnLocalInWorldSettingsChangedInLatency));
        }

        private void RaiseOnLocalInWorldSettingsChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalInWorldSettingsChangedListeners, nameof(VoiceRangeEventType.OnLocalInWorldSettingsChanged));
        }

        private void RaiseOnInHUDSettingsChangedInLatency(VoiceRangePlayerData playerDataForEvent)
        {
            this.playerDataForEvent = playerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onInHUDSettingsChangedInLatencyListeners, nameof(VoiceRangeEventType.OnInHUDSettingsChangedInLatency));
            this.playerDataForEvent = null; // To prevent misuse of the API.
            if (playerDataForEvent.core.isLocal)
                RaiseOnLocalInHUDSettingsChangedInLatency();
        }

        private void RaiseOnInHUDSettingsChanged(VoiceRangePlayerData playerDataForEvent)
        {
            this.playerDataForEvent = playerDataForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onInHUDSettingsChangedListeners, nameof(VoiceRangeEventType.OnInHUDSettingsChanged));
            this.playerDataForEvent = null; // To prevent misuse of the API.
            if (playerDataForEvent.core.isLocal)
                RaiseOnLocalInHUDSettingsChanged();
        }

        private void RaiseOnLocalInHUDSettingsChangedInLatency()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalInHUDSettingsChangedInLatencyListeners, nameof(VoiceRangeEventType.OnLocalInHUDSettingsChangedInLatency));
        }

        private void RaiseOnLocalInHUDSettingsChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalInHUDSettingsChangedListeners, nameof(VoiceRangeEventType.OnLocalInHUDSettingsChanged));
        }

        #endregion
    }
}
