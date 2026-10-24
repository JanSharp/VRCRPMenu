using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AssociatedEntityExtensionData(typeof(GMProxyExtensionData))]
    public class GMProxyExtension : EntityExtension
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][FindInParent] public CustomPickup pickup;
        [HideInInspector][FindInParent] public ItemExtension item;

        [System.NonSerialized] public GMProxyExtensionData data;

        [PermissionDefinitionReference(nameof(viewGMProxySpawnedByPDef))]
        public string viewGMProxySpawnedByPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewGMProxySpawnedByPDef;

        private bool defaultDisallowStealingHeld;
        private bool defaultDisallowStealingAttached;

        public override void OnInstantiate()
        {
            defaultDisallowStealingHeld = item.DisallowStealingHeld;
            defaultDisallowStealingAttached = item.DisallowStealingAttached;
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
            item.DisallowStealingHeld = defaultDisallowStealingHeld;
            item.DisallowStealingAttached = defaultDisallowStealingAttached;
        }

        public override void ApplyExtensionData()
        {
            bool hasViewSpawnedByPermission = viewGMProxySpawnedByPDef.valueForLocalPlayer;
            if (hasViewSpawnedByPermission)
            {
                item.DisallowStealingHeld = false;
                item.DisallowStealingAttached = false;
            }
            else
            {
                item.DisallowStealingHeld = defaultDisallowStealingHeld;
                item.DisallowStealingAttached = defaultDisallowStealingAttached;
            }

            if (!hasViewSpawnedByPermission || entityData.CreatedByPlayerData == null)
            {
                pickup.interactText = data.gmProxyDisplayName;
                return;
            }
            RPPlayerData player = playersBackendManager.GetRPPlayerData(entityData.CreatedByPlayerData.core);
            if (string.IsNullOrWhiteSpace(data.gmProxyDisplayName))
                pickup.interactText = $"Spawned by: {player.PlayerDisplayNameWithCharacterName}";
            else
                pickup.interactText = $"{data.gmProxyDisplayName}\nSpawned by: {player.PlayerDisplayNameWithCharacterName}";
        }
    }
}
