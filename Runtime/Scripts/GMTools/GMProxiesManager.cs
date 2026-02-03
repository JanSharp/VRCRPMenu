using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMProxiesManager : GMProxiesManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;

        [SerializeField] private string gmProxyEntityPrototypeName;

        private VRCPlayerApi localPlayer;

        #region GameState
        /// <summary>
        /// <para>While this is part of the game state, the order is non deterministic. Be very careful with
        /// how this is used to affect the game state.</para>
        /// </summary>
        private GMProxyExtensionData[] allGMProxies = new GMProxyExtensionData[ArrList.MinCapacity];
        private int allGMProxiesCount = 0;
        #endregion

        [PermissionDefinitionReference(nameof(spawnGMProxyPDef))]
        public string spawnGMProxyPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition spawnGMProxyPDef;

        [PermissionDefinitionReference(nameof(viewGMProxySpawnedByPDef))]
        public string viewGMProxySpawnedByPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewGMProxySpawnedByPDef;

        private bool isFirstPermissionResolve = true;
        private bool viewGMProxySpawnedByValue;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }

        /// <summary>
        /// <para>Internal API.</para>
        /// </summary>
        /// <param name="gmProxy"></param>
        public void RegisterGMProxy(GMProxyExtensionData gmProxy)
        {
            gmProxy.indexInAllGMProxies = allGMProxiesCount;
            ArrList.Add(ref allGMProxies, ref allGMProxiesCount, gmProxy);
        }

        /// <summary>
        /// <para>Internal API.</para>
        /// </summary>
        /// <param name="gmProxy"></param>
        public void DeregisterGMProxy(GMProxyExtensionData gmProxy)
        {
            int index = gmProxy.indexInAllGMProxies;
            if ((--allGMProxiesCount) == index)
                return;
            GMProxyExtensionData top = allGMProxies[allGMProxiesCount];
            top.indexInAllGMProxies = index;
            allGMProxies[index] = top;
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (!isFirstPermissionResolve && viewGMProxySpawnedByValue == viewGMProxySpawnedByPDef.valueForLocalPlayer)
                return;
            isFirstPermissionResolve = false;
            viewGMProxySpawnedByValue = viewGMProxySpawnedByPDef.valueForLocalPlayer;
            UpdateAllProxies();
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged)]
        public void OnRPPlayerDataOverriddenDisplayNameChanged()
        {
            if (viewGMProxySpawnedByValue) // No need to waste performance otherwise.
                UpdateAllProxiesCreatedByPlayer(entitySystem.GetPlayerData(playersBackendManager.RPPlayerDataForEvent.core));
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged)]
        public void OnRPPlayerDataCharacterNameChanged()
        {
            if (viewGMProxySpawnedByValue) // No need to waste performance otherwise.
                UpdateAllProxiesCreatedByPlayer(entitySystem.GetPlayerData(playersBackendManager.RPPlayerDataForEvent.core));
        }

        private void UpdateAllProxies()
        {
            for (int i = 0; i < allGMProxiesCount; i++)
            {
                GMProxyExtension ext = allGMProxies[i].ext;
                if (ext != null)
                    ext.ApplyExtensionData();
            }
        }

        private void UpdateAllProxiesCreatedByPlayer(EntitySystemPlayerData player)
        {
            for (int i = 0; i < allGMProxiesCount; i++)
            {
                GMProxyExtensionData data = allGMProxies[i];
                if (data.entityData.createdByPlayerData != player)
                    continue;
                GMProxyExtension ext = data.ext;
                if (ext != null)
                    ext.ApplyExtensionData();
            }
        }

        public override void CreateGMProxy(Vector3 position, Quaternion rotation, float scale, string displayName)
        {
            if (!lockstep.IsInitialized || !spawnGMProxyPDef.valueForLocalPlayer)
                return;
            if (!entitySystem.TryGetEntityPrototype(gmProxyEntityPrototypeName, out EntityPrototype prototype))
                return;
            lockstep.WriteString(displayName);
            lockstep.WriteFloat(scale);
            EntityData entityData = entitySystem.SendCustomCreateEntityIA(onCreateGMProxyIAId, prototype.Id, position, rotation);

            InitializeEntity(entityData, scale, displayName); // Latency hiding.
        }

        [HideInInspector][SerializeField] private uint onCreateGMProxyIAId;
        [LockstepInputAction(nameof(onCreateGMProxyIAId))]
        public void OnCreateGMProxyIA()
        {
            string displayName = lockstep.ReadString();
            float scale = lockstep.ReadFloat();
            EntityData entityData = entitySystem.ReadEntityInCustomCreateEntityIA(onEntityCreatedGetsRaisedLater: true);

            CorePlayerData sendingPlayer = playerDataManager.SendingPlayerData;
            if (!sendingPlayer.isLocal) // The sending local player already performed this initialization.
                InitializeEntity(entityData, scale, displayName);

            entitySystem.RaiseOnEntityCreatedInCustomCreateEntityIA(entityData);

            if (!permissionManager.PlayerHasPermission(sendingPlayer, spawnGMProxyPDef))
            {
                // Creating and instantly destroying is cleaner than having an entity just exist in the
                // latency state of one client without ever making it to the game state. That makes the life
                // cycle easier for every system using the entity system as well as the entity system itself.
                // Besides, this is so rare that it's basically never going to actually happen.
                entitySystem.DestroyEntity(entityData);
            }
        }

        private void InitializeEntity(EntityData entityData, float scale, string displayName)
        {
            GMProxyExtensionData ext = entityData.GetExtensionData<GMProxyExtensionData>(nameof(GMProxyExtensionData));
            entityData.scale = entityData.entityPrototype.DefaultScale * scale;
            ext.gmProxyDisplayName = displayName;
            // By the time this runs the entity for this entityData is guaranteed to not exist yet.
            // Therefore there is no need for any additional logic here, when the entity gets created it will
            // use the entityData, including the values that got populated above.
        }
    }
}
