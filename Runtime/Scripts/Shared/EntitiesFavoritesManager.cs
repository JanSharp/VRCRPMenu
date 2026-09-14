using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class EntitiesFavoritesManager : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;

        protected abstract DataDictionary RelevantPrototypeNamesLut { get; }

        protected abstract uint[] GetImportedFavoriteEntityIds(RPPlayerData player);
        protected abstract void SetImportedFavoriteEntityIds(RPPlayerData player, uint[] importedIds);
        protected abstract DataDictionary GetFavoriteEntityIdsLut(RPPlayerData player);
        protected abstract EntityPrototype[] GetFavoriteEntities(RPPlayerData player);
        protected abstract void SetFavoriteEntities(RPPlayerData player, EntityPrototype[] favorites);
        protected abstract int GetFavoriteEntitiesCount(RPPlayerData player);
        protected abstract void SetFavoriteEntitiesCount(RPPlayerData player, int favoritesCount);

        private RPPlayerData[] importedPlayers = new RPPlayerData[ArrList.MinCapacity];
        private int importedPlayersCount = 0;

        private void OnPlayerDataImported(RPPlayerData player)
        {
            ArrList.Add(ref importedPlayers, ref importedPlayersCount, player);
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp, Order = -100)]
        public void OnImportFinishingUp()
        {
            DataDictionary relevantPrototypeNamesLut = RelevantPrototypeNamesLut;
            for (int i = 0; i < importedPlayersCount; i++)
            {
                RPPlayerData player = importedPlayers[i];
                if (!player.CheckLiveliness()) // Since these are weak references must check liveliness.
                    continue;
                uint[] ids = GetImportedFavoriteEntityIds(player);
                if (ids == null) // The instance got deleted and a new one got created, reusing the pooled one. WannaBeClasses things.
                    continue;

                DataDictionary favoriteEntityIdsLut = GetFavoriteEntityIdsLut(player);
                favoriteEntityIdsLut.Clear();
                EntityPrototype[] favoriteEntities = GetFavoriteEntities(player);
                int favoriteEntitiesCount = 0;

                int count = ids.Length;
                for (int j = 0; j < count; j++)
                {
                    EntityPrototype entityPrototype = entitySystem.GetImportedPrototypeMetadata(ids[j]).entityPrototype;
                    if (entityPrototype == null || !relevantPrototypeNamesLut.ContainsKey(entityPrototype.PrototypeName))
                        continue;
                    ArrList.Add(ref favoriteEntities, ref favoriteEntitiesCount, entityPrototype);
                    favoriteEntityIdsLut.Add(entityPrototype.Id, true);
                }

                SetFavoriteEntities(player, favoriteEntities);
                SetFavoriteEntitiesCount(player, favoriteEntitiesCount);
                SetImportedFavoriteEntityIds(player, null);
            }
            ArrList.Clear(ref importedPlayers, ref importedPlayersCount);
        }

        public void SendAddFavoriteIA(RPPlayerData player, EntityPrototype prototype)
        {
            if (!lockstep.IsInitialized)
                return;
            playersBackendManager.WriteRPPlayerDataRef(player);
            entitySystem.WriteEntityPrototypeRef(prototype);
            lockstep.SendInputAction(addFavoriteEntityIAId);
        }

        [HideInInspector][SerializeField] private uint addFavoriteEntityIAId;
        [LockstepInputAction(nameof(addFavoriteEntityIAId))]
        public void OnAddFavoriteEntityIA()
        {
            RPPlayerData player = playersBackendManager.ReadRPPlayerDataRef();
            EntityPrototype prototype = entitySystem.ReadEntityPrototypeRef();
            if (player == null)
                return;
            var favoriteEntityIdsLut = GetFavoriteEntityIdsLut(player);
            if (favoriteEntityIdsLut.ContainsKey(prototype.Id))
                return;
            favoriteEntityIdsLut.Add(prototype.Id, true);

            // Disgusting.
            EntityPrototype[] favoriteEntities = GetFavoriteEntities(player);
            int favoriteEntitiesCount = GetFavoriteEntitiesCount(player);
            ArrList.Add(ref favoriteEntities, ref favoriteEntitiesCount, prototype);
            SetFavoriteEntities(player, favoriteEntities);
            SetFavoriteEntitiesCount(player, favoriteEntitiesCount);

            RaiseOnFavoriteAdded(player, prototype);
        }

        public void SendRemoveFavoriteIA(RPPlayerData player, EntityPrototype prototype)
        {
            if (!lockstep.IsInitialized)
                return;
            playersBackendManager.WriteRPPlayerDataRef(player);
            entitySystem.WriteEntityPrototypeRef(prototype);
            lockstep.SendInputAction(removeFavoriteEntityIAId);
        }

        [HideInInspector][SerializeField] private uint removeFavoriteEntityIAId;
        [LockstepInputAction(nameof(removeFavoriteEntityIAId))]
        public void OnRemoveFavoriteEntityIA()
        {
            RPPlayerData player = playersBackendManager.ReadRPPlayerDataRef();
            EntityPrototype prototype = entitySystem.ReadEntityPrototypeRef();
            if (player == null)
                return;
            if (!GetFavoriteEntityIdsLut(player).Remove(prototype.Id))
                return;

            // Disgusting.
            EntityPrototype[] favoriteEntities = GetFavoriteEntities(player);
            int favoriteEntitiesCount = GetFavoriteEntitiesCount(player);
            ArrList.Remove(ref favoriteEntities, ref favoriteEntitiesCount, prototype);
            SetFavoriteEntities(player, favoriteEntities);
            SetFavoriteEntitiesCount(player, favoriteEntitiesCount);

            RaiseOnFavoriteRemoved(player, prototype);
        }

        #region Player Data Serialization

        private void WriteFavoriteItems(RPPlayerData player)
        {
            EntityPrototype[] favoriteEntities = GetFavoriteEntities(player);
            int count = GetFavoriteEntitiesCount(player);
            lockstep.WriteSmallUInt((uint)count);
            for (int i = 0; i < count; i++)
                lockstep.WriteSmallUInt(favoriteEntities[i].Id);
        }

        private void ReadFavoriteItems(RPPlayerData player, bool isImport, bool discard)
        {
            if (discard)
            {
                int count = (int)lockstep.ReadSmallUInt();
                for (int i = 0; i < count; i++)
                    lockstep.ReadSmallUInt();
                return;
            }

            if (isImport)
            {
                int count = (int)lockstep.ReadSmallUInt();
                uint[] importedFavoriteIds = new uint[count];
                for (int i = 0; i < count; i++)
                    importedFavoriteIds[i] = lockstep.ReadSmallUInt();
                SetImportedFavoriteEntityIds(player, importedFavoriteIds);
                OnPlayerDataImported(player);
            }
            else
            {
                DataDictionary favoriteEntityIdsLut = GetFavoriteEntityIdsLut(player);
                EntityPrototype[] favoriteEntities = GetFavoriteEntities(player);
                int count = (int)lockstep.ReadSmallUInt();
                ArrList.EnsureCapacity(ref favoriteEntities, count);
                for (int i = 0; i < count; i++)
                {
                    uint id = lockstep.ReadSmallUInt();
                    favoriteEntities[i] = entitySystem.GetEntityPrototype(id);
                    favoriteEntityIdsLut.Add(id, true);
                }
                SetFavoriteEntities(player, favoriteEntities);
                SetFavoriteEntitiesCount(player, count);
            }
        }

        protected abstract bool GetFavoritesAreIncluded(PlayersBackendImportExportOptions options);

        public void SerializeFavoritesForPlayer(RPPlayerData player, bool isExport)
        {
            PlayersBackendImportExportOptions exportOptions = playersBackendManager.ExportOptions;

            if (!isExport || GetFavoritesAreIncluded(exportOptions))
                WriteFavoriteItems(player);
        }

        public void DeserializeFavoritesForPlayer(RPPlayerData player, bool isImport)
        {
            PlayersBackendImportExportOptions optionsFromExport = playersBackendManager.OptionsFromExport;
            PlayersBackendImportExportOptions importOptions = playersBackendManager.ImportOptions;

            if (!isImport)
                ReadFavoriteItems(player, isImport, discard: false);
            else if (GetFavoritesAreIncluded(optionsFromExport))
                ReadFavoriteItems(player, isImport, discard: !GetFavoritesAreIncluded(importOptions));
        }

        #endregion

        #region EventDispatcher

        protected abstract UdonSharpBehaviour[] OnFavoriteAddedListeners { get; set; }
        protected abstract UdonSharpBehaviour[] OnFavoriteRemovedListeners { get; set; }
        protected abstract string OnFavoriteAddedEventName { get; }
        protected abstract string OnFavoriteRemovedEventName { get; }

        private RPPlayerData playerForEvent;
        public RPPlayerData PlayerForEvent => playerForEvent;
        private EntityPrototype entityPrototypeForEvent;
        public EntityPrototype EntityPrototypeForEvent => entityPrototypeForEvent;

        private void RaiseOnFavoriteAdded(RPPlayerData playerForEvent, EntityPrototype entityPrototypeForEvent)
        {
            this.playerForEvent = playerForEvent;
            this.entityPrototypeForEvent = entityPrototypeForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            UdonSharpBehaviour[] listeners = OnFavoriteAddedListeners;
            JanSharp.CustomRaisedEvents.Raise(ref listeners, OnFavoriteAddedEventName);
            OnFavoriteAddedListeners = listeners;
            this.playerForEvent = null; // To prevent misuse of the API.
            this.entityPrototypeForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnFavoriteRemoved(RPPlayerData playerForEvent, EntityPrototype entityPrototypeForEvent)
        {
            this.playerForEvent = playerForEvent;
            this.entityPrototypeForEvent = entityPrototypeForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            UdonSharpBehaviour[] listeners = OnFavoriteRemovedListeners;
            JanSharp.CustomRaisedEvents.Raise(ref listeners, OnFavoriteRemovedEventName);
            OnFavoriteRemovedListeners = listeners;
            this.playerForEvent = null; // To prevent misuse of the API.
            this.entityPrototypeForEvent = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
