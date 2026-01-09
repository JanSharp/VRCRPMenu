namespace JanSharp
{
    public abstract class PerPlayerDynamicData : PlayerData
    {
        public override bool SupportsImportExport => false;

        public abstract string DynamicDataClassName { get; }

        #region GameState
        private DynamicData[] localDynamicData = new DynamicData[ArrList.MinCapacity];
        private int localDynamicDataCount = 0;
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
                localDynamicData[i] = (DynamicData)lockstep.ReadCustomClass(DynamicDataClassName);
        }
    }
}
