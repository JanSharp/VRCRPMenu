using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMProxyExtensionData : EntityExtensionData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] private GMProxiesManagerAPI gmProxiesManager;

        #region GameState
        [System.NonSerialized] public string gmProxyDisplayName;
        #endregion

        [System.NonSerialized] public int indexInAllGMProxies;
        [System.NonSerialized] public GMProxyExtension ext;

        public override void InitFromDefault(EntityExtension entityExtension)
        {
            gmProxyDisplayName = ((GMProxyExtension)entityExtension).pickup.interactText;
        }

        public override void InitFromPreInstantiated(EntityExtension entityExtension)
        {
            gmProxyDisplayName = ((GMProxyExtension)entityExtension).pickup.interactText;
            ((Internal.GMProxiesManager)gmProxiesManager).RegisterGMProxy(this);
        }

        public override void InitBeforeDeserialization()
        {
            ((Internal.GMProxiesManager)gmProxiesManager).RegisterGMProxy(this);
        }

        public override void OnEntityExtensionDataCreated()
        {
            ((Internal.GMProxiesManager)gmProxiesManager).RegisterGMProxy(this);
        }

        public override void OnEntityExtensionDataDestroyed()
        {
            ((Internal.GMProxiesManager)gmProxiesManager).DeregisterGMProxy(this);
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteString(gmProxyDisplayName);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            gmProxyDisplayName = lockstep.ReadString();
        }
    }
}
