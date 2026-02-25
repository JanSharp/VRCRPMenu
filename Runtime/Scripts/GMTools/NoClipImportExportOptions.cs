using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipImportExportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public bool includeNoClipSettings = true;

        public override LockstepGameStateOptionsData Clone()
        {
            var clone = WannaBeClasses.New<NoClipImportExportOptions>(nameof(NoClipImportExportOptions));
            clone.includeNoClipSettings = includeNoClipSettings;
            return clone;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(includeNoClipSettings);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(out includeNoClipSettings);
        }
    }
}
