using VRC.SDK3.Data;

namespace JanSharp
{
    public abstract class PerPlayerDynamicData : PlayerData
    {
        public override bool SupportsImportExport => false;

        public abstract string DynamicDataClassName { get; }

        #region GameState
        [System.NonSerialized] public DataDictionary localDynamicDataByName = new DataDictionary();
        [System.NonSerialized] public DynamicData[] localDynamicData = new DynamicData[ArrList.MinCapacity];
        [System.NonSerialized] public int localDynamicDataCount = 0;
        #endregion

        public override bool PersistPlayerDataWhileOffline()
        {
            return localDynamicDataCount != 0;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteSmallUInt((uint)localDynamicDataCount);
            for (int i = 0; i < localDynamicDataCount; i++)
                lockstep.WriteCustomClass(localDynamicData[i]);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            localDynamicDataCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref localDynamicData, localDynamicDataCount);
            for (int i = 0; i < localDynamicDataCount; i++)
            {
                DynamicData data = (DynamicData)lockstep.ReadCustomClass(DynamicDataClassName);
                localDynamicDataByName.Add(data.dataName, data);
                localDynamicData[i] = data;
            }
        }
    }
}
