using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangePlayerData : PlayerData
    {
        public override string PlayerDataInternalName => "jansharp.voice-range";
        public override string PlayerDataDisplayName => "Voice Range Data";
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;

        #region LatencyState
        [System.NonSerialized] public DataDictionary latencyHiddenUniqueIds = new DataDictionary();
        [System.NonSerialized] public int latencyVoiceRangeIndex;
        [System.NonSerialized] public uint latencyShowInWorldMask;
        [System.NonSerialized] public VoiceRangeVisualizationType latencyWorldVisualType;
        [System.NonSerialized] public uint latencyShowInHUDMask;
        [System.NonSerialized] public VoiceRangeVisualizationType latencyHUDVisualType;
        #endregion

        #region GameState
        [System.NonSerialized] public int voiceRangeIndex;
        [System.NonSerialized] public uint showInWorldMask;
        [System.NonSerialized] public VoiceRangeVisualizationType worldVisualType;
        [System.NonSerialized] public uint showInHUDMask;
        [System.NonSerialized] public VoiceRangeVisualizationType hudVisualType;
        #endregion

        public override void OnPlayerDataInit(bool isAboutToBeImported)
        {
            voiceRangeIndex = voiceRangeManager.DefaultVoiceRangeIndex;
            latencyVoiceRangeIndex = voiceRangeIndex;
            if (isAboutToBeImported)
                return;
            showInWorldMask = voiceRangeManager.DefaultShowInWorldMask;
            worldVisualType = voiceRangeManager.DefaultWorldVisualType;
            showInHUDMask = voiceRangeManager.DefaultShowInHUDMask;
            hudVisualType = voiceRangeManager.DefaultHUDVisualType;
            latencyShowInWorldMask = showInWorldMask;
            latencyWorldVisualType = worldVisualType;
            latencyShowInHUDMask = showInHUDMask;
            latencyHUDVisualType = hudVisualType;
        }

        public override void OnPlayerDataLeft()
        {
            voiceRangeIndex = voiceRangeManager.DefaultVoiceRangeIndex;
            latencyVoiceRangeIndex = voiceRangeIndex;
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return showInWorldMask != voiceRangeManager.DefaultShowInWorldMask
                || worldVisualType != voiceRangeManager.DefaultWorldVisualType
                || showInHUDMask != voiceRangeManager.DefaultShowInHUDMask
                || hudVisualType != voiceRangeManager.DefaultHUDVisualType;
        }

        public override void Serialize(bool isExport)
        {
            if (!isExport)
                lockstep.WriteSmallUInt((uint)voiceRangeIndex);
            lockstep.WriteSmallUInt(showInWorldMask);
            lockstep.WriteByte((byte)worldVisualType);
            lockstep.WriteSmallUInt(showInHUDMask);
            lockstep.WriteByte((byte)hudVisualType);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            if (!isImport)
            {
                voiceRangeIndex = (int)lockstep.ReadSmallUInt();
                latencyVoiceRangeIndex = voiceRangeIndex;
            }
            else
                latencyHiddenUniqueIds.Clear(); // Empty when not importing anyway, no need to clear there.
            showInWorldMask = lockstep.ReadSmallUInt();
            worldVisualType = (VoiceRangeVisualizationType)lockstep.ReadByte();
            showInHUDMask = lockstep.ReadSmallUInt();
            hudVisualType = (VoiceRangeVisualizationType)lockstep.ReadByte();
            latencyShowInWorldMask = showInWorldMask;
            latencyWorldVisualType = worldVisualType;
            latencyShowInHUDMask = showInHUDMask;
            latencyHUDVisualType = hudVisualType;
        }
    }
}
