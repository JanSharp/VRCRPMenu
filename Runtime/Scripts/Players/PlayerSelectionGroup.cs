using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionGroup : DynamicData
    {
        #region GameState
        [System.NonSerialized] public CorePlayerData[] selectedPlayers;
        #endregion

        public override void Serialize(bool isExport)
        {
            base.Serialize(isExport);
            int count = selectedPlayers.Length;
            lockstep.WriteSmallUInt((uint)count);
            for (int i = 0; i < count; i++)
                playerDataManager.WriteCorePlayerDataRef(selectedPlayers[i]);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            base.Deserialize(isImport, importedDataVersion);
            int count = (int)lockstep.ReadSmallUInt();
            selectedPlayers = new CorePlayerData[count];
            for (int i = 0; i < count; i++)
                selectedPlayers[i] = playerDataManager.ReadCorePlayerDataRef();
        }
    }
}
