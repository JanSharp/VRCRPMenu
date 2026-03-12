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
            int actualCount = 0;
            for (int i = 0; i < count; i++)
            {
                CorePlayerData corePlayerData = playerDataManager.ReadCorePlayerDataRef(isImport);
                if (corePlayerData != null)
                    selectedPlayers[actualCount++] = corePlayerData;
            }
            if (actualCount == count)
                return;
            CorePlayerData[] resized = new CorePlayerData[actualCount];
            System.Array.Copy(selectedPlayers, resized, actualCount);
            selectedPlayers = resized;
        }
    }
}
