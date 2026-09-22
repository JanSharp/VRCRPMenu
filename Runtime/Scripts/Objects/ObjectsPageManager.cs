using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsPageManager : ObjectsPageManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;

        /// <summary>
        /// <para><see cref="string"/> entityPrototypeName => <see langword="true"/></para>
        /// </summary>
        private DataDictionary objectPrototypeNamesLut = new DataDictionary();
        public override DataDictionary ObjectPrototypeNamesLut => objectPrototypeNamesLut;

        private EntityPrototype[] objectPrototypes = new EntityPrototype[ArrList.MinCapacity];
        private int objectPrototypesCount = 0;
        public override EntityPrototype[] ObjectPrototypesRaw => objectPrototypes;
        public override int ObjectPrototypesCount => objectPrototypesCount;
        public override EntityPrototype GetObjectPrototype(int index) => objectPrototypes[index];

        [SerializeField] private Transform creationPreviewsContainer;

        [SerializeField] private LayerMask objectsCollisionLayers;
        [SerializeField] private float maxRaycastDistance;
        [SerializeField] private Transform laserPointerRoot;
        [SerializeField] private EntityTransformGizmoBridge entityTransformGizmo;

        public override LayerMask ObjectsCollisionLayers => objectsCollisionLayers;
        public override float MaxRaycastDistance => maxRaycastDistance;
        public override Transform LaserPointerRoot => laserPointerRoot;
        public override EntityTransformGizmoBridge EntityTransformGizmo => entityTransformGizmo;

        private DataDictionary prototypePreviewInstsLut = new DataDictionary();
        private uint localPlayerId;

        private void Start()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;

            EntityPrototype[] prototypes = entitySystem.EntityPrototypes;
            foreach (EntityPrototype prototype in prototypes)
            {
                string[] classNames = prototype.ExtensionDataClassNames;
                if (System.Array.IndexOf(classNames, nameof(ObjectEntityExtensionData)) != -1)
                {
                    objectPrototypeNamesLut.Add(prototype.PrototypeName, true);
                    ArrList.Add(ref objectPrototypes, ref objectPrototypesCount, prototype);
                }
            }
        }

        public override GameObject GetCreationPreviewInstanceForPrototype(EntityPrototype prototype)
        {
            if (prototypePreviewInstsLut.TryGetValue(prototype, out DataToken previewToken))
                return (GameObject)previewToken.Reference;

            string[] classNames = prototype.ExtensionDataClassNames;
            int extensionIndex = System.Array.IndexOf(classNames, nameof(ObjectEntityExtensionData));
            if (extensionIndex == -1)
            {
                Debug.LogError($"[RPMenu] Attempt to get an object creation preview instance for the "
                    + $"entity prototype '{prototype.PrototypeName}', however said prototype does "
                    + $"not have the {nameof(ObjectEntityExtension)}.", prototype);
                return null;
            }

            ObjectEntityExtension objectExt = (ObjectEntityExtension)prototype.DefaultEntityInst.extensions[extensionIndex];
            GameObject preview = Instantiate(objectExt.creationPreviewPrefab, creationPreviewsContainer);
            preview.SetActive(false);
            prototypePreviewInstsLut.Add(prototype, preview);
            return preview;
        }

        public override EntityData SendCreateScaledEntityIA(
            EntityPrototype prototype,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (!lockstep.IsInitialized)
                return null;
            lockstep.WriteVector3(scale);
            EntityData entityData = entitySystem.SendCustomCreateEntityIA(onCreateScaledEntityIAId, prototype.Id, position, rotation);

            InitializeScaledEntity(entityData, scale); // Latency hiding.
            return entityData;
        }

        [HideInInspector][SerializeField] private uint onCreateScaledEntityIAId;
        [LockstepInputAction(nameof(onCreateScaledEntityIAId))]
        public void OnCreateScaledEntityIA()
        {
            Vector3 scale = lockstep.ReadVector3();
            EntityData entityData = entitySystem.ReadEntityInCustomCreateEntityIA(onEntityCreatedGetsRaisedLater: true);

            if (lockstep.SendingPlayerId != localPlayerId) // The sending local player already performed this initialization.
                InitializeScaledEntity(entityData, scale);

            entitySystem.RaiseOnEntityCreatedInCustomCreateEntityIA(entityData);
        }

        private void InitializeScaledEntity(EntityData entityData, Vector3 scale)
        {
            entityData.scale = scale;
            // By the time this runs the entity for this entityData is guaranteed to not exist yet.
            // Therefore there is no need for any additional logic here, when the entity gets created it will
            // use the entityData, including the values that got populated above.
        }
    }
}
