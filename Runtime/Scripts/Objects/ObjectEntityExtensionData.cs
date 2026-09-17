using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectEntityExtensionData : EntityExtensionData
    {
        public override bool SupportsImportExport => false;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public ObjectEntityExtension ext;

        public override bool WannaBeClassSupportsPooling => true;
        public override void ResetWannaBeClassToDefault()
        {
            base.ResetWannaBeClassToDefault();
            ext = default;
        }

        public override void InitBeforeDeserialization() { }

        public override void InitFromPreInstantiated(EntityExtension entityExtension) { }

        public override void InitFromDefault(EntityExtension entityExtension) { }

        public override void Serialize(bool isExport) { }

        public override void Deserialize(bool isImport, uint importedDataVersion) { }
    }
}
