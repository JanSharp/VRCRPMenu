using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AssociatedEntityExtensionData(typeof(GMProxyExtensionData))]
    public class GMProxyExtension : EntityExtension
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;

        [System.NonSerialized] public CustomPickup pickup;
        [System.NonSerialized] public GMProxyExtensionData data;

        [PermissionDefinitionReference(nameof(viewGMProxySpawnedByPDef))]
        public string viewGMProxySpawnedByPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewGMProxySpawnedByPDef;

        public override void OnInstantiate()
        {
            pickup = GetComponent<CustomPickup>();
        }

        public override void AssociateWithExtensionData()
        {
            data = (GMProxyExtensionData)extensionData;
            data.ext = this;
            ApplyExtensionData();
        }

        public override void DisassociateFromExtensionDataAndReset(EntityExtension defaultExtension)
        {
            data.ext = null;
            data = null;
            pickup.interactText = ((GMProxyExtension)defaultExtension).pickup.interactText;
        }

        public override void ApplyExtensionData()
        {
            if (!viewGMProxySpawnedByPDef.valueForLocalPlayer || entityData.createdByPlayerData == null)
            {
                pickup.interactText = data.gmProxyDisplayName;
                return;
            }
            RPPlayerData player = playersBackendManager.GetRPPlayerData(entityData.createdByPlayerData.core);
            if (string.IsNullOrWhiteSpace(data.gmProxyDisplayName))
                pickup.interactText = $"Spawned by: {player.PlayerDisplayNameWithCharacterName}";
            else
                pickup.interactText = $"{data.gmProxyDisplayName}\nSpawned by: {player.PlayerDisplayNameWithCharacterName}";
        }
    }
}
