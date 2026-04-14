using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuSettingsImportExportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public bool includeMenuSettings = true;

        public override bool WannaBeClassSupportsPooling => true;
        public override void ResetWannaBeClassToDefault()
        {
            base.ResetWannaBeClassToDefault();
            includeMenuSettings = true;
        }

        public override LockstepGameStateOptionsData Clone()
        {
            var clone = wannaBeClasses.New<MenuSettingsImportExportOptions>(nameof(MenuSettingsImportExportOptions));
            clone.includeMenuSettings = includeMenuSettings;
            return clone;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(includeMenuSettings);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(out includeMenuSettings);
        }
    }
}
