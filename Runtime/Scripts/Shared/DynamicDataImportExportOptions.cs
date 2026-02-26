using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DynamicDataImportExportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public bool includeGlobal = true;
        [System.NonSerialized] public bool includePerPlayer = true;

        public override LockstepGameStateOptionsData Clone()
        {
            var clone = WannaBeClasses.New<DynamicDataImportExportOptions>(nameof(DynamicDataImportExportOptions));
            clone.includeGlobal = includeGlobal;
            clone.includePerPlayer = includePerPlayer;
            return clone;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(
                includeGlobal,
                includePerPlayer);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(
                out includeGlobal,
                out includePerPlayer);
        }
    }
}
