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
        [System.NonSerialized] public int latencyCurrentRangeIndex;
        [System.NonSerialized] public uint latencyShowInWorldMask;
        [System.NonSerialized] public VoiceRangeVisualizationType latencyWorldVisualType = VoiceRangeVisualizationType.Default;
        [System.NonSerialized] public uint latencyShowInHUDMask;
        [System.NonSerialized] public VoiceRangeVisualizationType latencyHudVisualType = VoiceRangeVisualizationType.Default;
        #endregion

        #region GameState
        [System.NonSerialized] public int currentRangeIndex;
        [System.NonSerialized] public uint showInWorldMask;
        [System.NonSerialized] public VoiceRangeVisualizationType worldVisualType = VoiceRangeVisualizationType.Default;
        [System.NonSerialized] public uint showInHUDMask;
        [System.NonSerialized] public VoiceRangeVisualizationType hudVisualType = VoiceRangeVisualizationType.Default;
        #endregion

        public override void OnPlayerDataInit(bool isAboutToBeImported)
        {
            currentRangeIndex = voiceRangeManager.DefaultVoiceRangeIndex;
            latencyCurrentRangeIndex = currentRangeIndex;
            if (isAboutToBeImported)
                return;
            showInWorldMask = voiceRangeManager.DefaultShowInWorldMask;
            showInHUDMask = voiceRangeManager.DefaultShowInHUDMask;
            latencyShowInWorldMask = showInWorldMask;
            latencyShowInHUDMask = showInHUDMask;
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return showInWorldMask != voiceRangeManager.DefaultShowInWorldMask
                || worldVisualType != VoiceRangeVisualizationType.Default
                || showInHUDMask != voiceRangeManager.DefaultShowInHUDMask
                || hudVisualType != VoiceRangeVisualizationType.Default;
        }

        public override void Serialize(bool isExport)
        {
            if (!isExport)
                lockstep.WriteSmallUInt((uint)currentRangeIndex);
            lockstep.WriteSmallUInt(showInWorldMask);
            lockstep.WriteByte((byte)worldVisualType);
            lockstep.WriteSmallUInt(showInHUDMask);
            lockstep.WriteByte((byte)hudVisualType);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            if (!isImport)
                currentRangeIndex = (int)lockstep.ReadSmallUInt();
            else
                latencyHiddenUniqueIds.Clear(); // Empty when not importing anyway, no need to clear there.
            showInWorldMask = lockstep.ReadSmallUInt();
            worldVisualType = (VoiceRangeVisualizationType)lockstep.ReadByte();
            showInHUDMask = lockstep.ReadSmallUInt();
            hudVisualType = (VoiceRangeVisualizationType)lockstep.ReadByte();
            latencyShowInWorldMask = showInWorldMask;
            latencyWorldVisualType = worldVisualType;
            latencyShowInHUDMask = showInHUDMask;
            latencyHudVisualType = hudVisualType;
        }
    }
}
