using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeImportExportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public bool includeVoiceRangeSettings = true;

        public override LockstepGameStateOptionsData Clone()
        {
            var clone = WannaBeClasses.New<VoiceRangeImportExportOptions>(nameof(VoiceRangeImportExportOptions));
            clone.includeVoiceRangeSettings = includeVoiceRangeSettings;
            return clone;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(includeVoiceRangeSettings);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(out includeVoiceRangeSettings);
        }
    }
}
