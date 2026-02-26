using VRC.SDK3.Data;

namespace JanSharp
{
    public abstract class PerPlayerDynamicData : PlayerData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        public abstract string DynamicDataClassName { get; }
        public abstract DynamicDataManager DataManager { get; }

        #region GameState
        [System.NonSerialized] public DataDictionary localDynamicDataByName = new DataDictionary();
        [System.NonSerialized] public DynamicData[] localDynamicData = new DynamicData[WannaBeArrList.MinCapacity];
        [System.NonSerialized] public int localDynamicDataCount = 0;
        #endregion

        public override void OnPlayerDataUninit(bool force)
        {
            DataDictionary allDynamicDataById = DataManager.allDynamicDataById;
            for (int i = localDynamicDataCount - 1; i >= 0; i--)
            {
                DynamicData data = localDynamicData[i];
                allDynamicDataById.Remove(data.id);
                data.DecrementRefsCount();
            }
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return localDynamicDataCount != 0;
        }

        private void WriteLocalDynamicData(bool isExport)
        {
            lockstep.WriteSmallUInt((uint)localDynamicDataCount);
            for (int i = 0; i < localDynamicDataCount; i++)
                lockstep.WriteCustomClass(localDynamicData[i], isExport);
        }

        private void ReadLocalDynamicData(bool isImport, bool discard)
        {
            string dynamicDataClassName = DynamicDataClassName;
            DynamicDataManager dataManager = DataManager;

            if (discard)
            {
                int count = (int)lockstep.ReadSmallUInt();
                if (count == 0)
                    return;
                DynamicData dummy = (DynamicData)WannaBeClasses.NewDynamic(dynamicDataClassName);
                for (int i = 0; i < count; i++)
                    lockstep.ReadCustomClass(dummy, isImport);
                dummy.Delete();
                return;
            }

            if (isImport)
            {
                localDynamicDataByName.Clear();
                for (int i = 0; i < localDynamicDataCount; i++)
                {
                    DynamicData data = localDynamicData[i];
                    dataManager.allDynamicDataById.Remove(data.id);
                    data.DecrementRefsCount();
                }
            }

            localDynamicDataCount = (int)lockstep.ReadSmallUInt();
            WannaBeArrList.EnsureCapacity(ref localDynamicData, localDynamicDataCount);
            for (int i = 0; i < localDynamicDataCount; i++)
            {
                DynamicData data = (DynamicData)lockstep.ReadCustomClass(dynamicDataClassName, isImport);
                if (isImport)
                    data.id = dataManager.GetNextDynamicDataId();
                dataManager.allDynamicDataById.Add(data.id, data);
                localDynamicDataByName.Add(data.dataName, data);
                localDynamicData[i] = data;
            }
        }

        public override void Serialize(bool isExport)
        {
            if (!isExport || DataManager.optionsGS.ExportOptions.includePerPlayer)
                WriteLocalDynamicData(isExport);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            if (!isImport)
                ReadLocalDynamicData(isImport, discard: false);
            else if (DataManager.optionsGS.OptionsFromExport.includePerPlayer)
                ReadLocalDynamicData(isImport, discard: !DataManager.optionsGS.ImportOptions.includePerPlayer);
        }
    }
}
