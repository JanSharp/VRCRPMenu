using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemsPageManager : ItemsPageManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private EntityPrototype[] itemPrototypes = new EntityPrototype[ArrList.MinCapacity];
        private int itemPrototypesCount = 0;
        public override EntityPrototype[] ItemPrototypesRaw => itemPrototypes;
        public override int ItemPrototypesCount => itemPrototypesCount;
        public override EntityPrototype GetItemPrototype(int index) => itemPrototypes[index];

        [PermissionDefinitionReference(nameof(spawnItemPDef))]
        public string spawnItemPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition spawnItemPDef;

        private void Start()
        {
            EntityPrototype[] prototypes = entitySystem.EntityPrototypes;
            foreach (EntityPrototype prototype in prototypes)
            {
                string[] classNames = prototype.ExtensionDataClassNames;
                if (System.Array.IndexOf(classNames, nameof(ItemExtensionData)) != -1
                    && System.Array.IndexOf(classNames, nameof(GMProxyExtensionData)) == -1)
                {
                    ArrList.Add(ref itemPrototypes, ref itemPrototypesCount, prototype);
                }
            }
        }

        public override void CreateItem(EntityPrototype prototype, Vector3 position, Quaternion rotation, float scale)
        {
            if (!lockstep.IsInitialized || !spawnItemPDef.valueForLocalPlayer)
                return;
            lockstep.WriteFloat(scale);
            EntityData entityData = entitySystem.SendCustomCreateEntityIA(onCreateItemIAId, prototype.Id, position, rotation);

            InitializeEntity(entityData, scale); // Latency hiding.
        }

        [HideInInspector][SerializeField] private uint onCreateItemIAId;
        [LockstepInputAction(nameof(onCreateItemIAId))]
        public void OnCreateItemIA()
        {
            float scale = lockstep.ReadFloat();
            EntityData entityData = entitySystem.ReadEntityInCustomCreateEntityIA(onEntityCreatedGetsRaisedLater: true);

            CorePlayerData sendingPlayer = playerDataManager.SendingPlayerData;
            if (!sendingPlayer.isLocal) // The sending local player already performed this initialization.
                InitializeEntity(entityData, scale);

            entitySystem.RaiseOnEntityCreatedInCustomCreateEntityIA(entityData);

            if (!permissionManager.PlayerHasPermission(sendingPlayer, spawnItemPDef))
            {
                // Creating and instantly destroying is cleaner than having an entity just exist in the
                // latency state of one client without ever making it to the game state. That makes the life
                // cycle easier for every system using the entity system as well as the entity system itself.
                // Besides, this is so rare that it's basically never going to actually happen.
                entitySystem.DestroyEntity(entityData);
            }
        }

        private void InitializeEntity(EntityData entityData, float scale)
        {
            entityData.scale = entityData.entityPrototype.DefaultScale * scale;
            // By the time this runs the entity for this entityData is guaranteed to not exist yet.
            // Therefore there is no need for any additional logic here, when the entity gets created it will
            // use the entityData, including the values that got populated above.
        }
    }
}
