using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemsFavoritesManager : ItemsFavoritesManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemsPageManagerAPI itemsPageManager;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;

        private RPPlayerData[] importedPlayers = new RPPlayerData[ArrList.MinCapacity];
        private int importedPlayersCount = 0;

        public void OnPlayerDataImported(RPPlayerData player)
        {
            ArrList.Add(ref importedPlayers, ref importedPlayersCount, player);
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp, Order = -100)]
        public void OnImportFinishingUp()
        {
            DataDictionary itemPrototypeNamesLut = itemsPageManager.ItemPrototypeNamesLut;
            for (int i = 0; i < importedPlayersCount; i++)
            {
                RPPlayerData player = importedPlayers[i];
                DataDictionary favoriteItemIdsLut = player.favoriteItemIdsLut;
                favoriteItemIdsLut.Clear();
                EntityPrototype[] favoriteItems = player.favoriteItems;
                int favoriteItemsCount = 0;

                uint[] ids = player.importedFavoriteItemIds;
                int count = ids.Length;
                for (int j = 0; j < count; j++)
                {
                    EntityPrototype entityPrototype = entitySystem.GetImportedPrototypeMetadata(ids[j]).entityPrototype;
                    if (entityPrototype == null || !itemPrototypeNamesLut.ContainsKey(entityPrototype.PrototypeName))
                        continue;
                    ArrList.Add(ref favoriteItems, ref favoriteItemsCount, entityPrototype);
                    favoriteItemIdsLut.Add(entityPrototype.Id, true);
                }

                player.favoriteItems = favoriteItems;
                player.favoriteItemsCount = favoriteItemsCount;
                player.importedFavoriteItemIds = null;
            }
            ArrList.Clear(ref importedPlayers, ref importedPlayersCount);
        }

        public override void SendAddFavoriteItemIA(RPPlayerData player, EntityPrototype prototype)
        {
            if (!lockstep.IsInitialized)
                return;
            playersBackendManager.WriteRPPlayerDataRef(player);
            entitySystem.WriteEntityPrototypeRef(prototype);
            lockstep.SendInputAction(addFavoritePlayerIAId);
        }

        [HideInInspector][SerializeField] private uint addFavoritePlayerIAId;
        [LockstepInputAction(nameof(addFavoritePlayerIAId))]
        public void OnAddFavoritePlayerIA()
        {
            RPPlayerData player = playersBackendManager.ReadRPPlayerDataRef();
            EntityPrototype prototype = entitySystem.ReadEntityPrototypeRef();
            if (player == null)
                return;
            if (player.favoriteItemIdsLut.ContainsKey(prototype.Id))
                return;
            player.favoriteItemIdsLut.Add(prototype.Id, true);
            ArrList.Add(ref player.favoriteItems, ref player.favoriteItemsCount, prototype);
            RaiseOnItemFavoriteAdded(player, prototype);
        }

        public override void SendRemoveFavoriteItemIA(RPPlayerData player, EntityPrototype prototype)
        {
            if (!lockstep.IsInitialized)
                return;
            playersBackendManager.WriteRPPlayerDataRef(player);
            entitySystem.WriteEntityPrototypeRef(prototype);
            lockstep.SendInputAction(removeFavoritePlayerIAId);
        }

        [HideInInspector][SerializeField] private uint removeFavoritePlayerIAId;
        [LockstepInputAction(nameof(removeFavoritePlayerIAId))]
        public void OnRemoveFavoritePlayerIA()
        {
            RPPlayerData player = playersBackendManager.ReadRPPlayerDataRef();
            EntityPrototype prototype = entitySystem.ReadEntityPrototypeRef();
            if (player == null)
                return;
            if (!player.favoriteItemIdsLut.Remove(prototype.Id))
                return;
            ArrList.Remove(ref player.favoriteItems, ref player.favoriteItemsCount, prototype);
            RaiseOnItemFavoriteRemoved(player, prototype);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onItemFavoriteAddedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onItemFavoriteRemovedListeners;

        private RPPlayerData playerForEvent;
        public override RPPlayerData PlayerForEvent => playerForEvent;
        private EntityPrototype entityPrototypeForEvent;
        public override EntityPrototype EntityPrototypeForEvent => entityPrototypeForEvent;

        private void RaiseOnItemFavoriteAdded(RPPlayerData playerForEvent, EntityPrototype entityPrototypeForEvent)
        {
            this.playerForEvent = playerForEvent;
            this.entityPrototypeForEvent = entityPrototypeForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onItemFavoriteAddedListeners, nameof(ItemsFavoritesEventType.OnItemFavoriteAdded));
            this.playerForEvent = null; // To prevent misuse of the API.
            this.entityPrototypeForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnItemFavoriteRemoved(RPPlayerData playerForEvent, EntityPrototype entityPrototypeForEvent)
        {
            this.playerForEvent = playerForEvent;
            this.entityPrototypeForEvent = entityPrototypeForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onItemFavoriteRemovedListeners, nameof(ItemsFavoritesEventType.OnItemFavoriteRemoved));
            this.playerForEvent = null; // To prevent misuse of the API.
            this.entityPrototypeForEvent = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
