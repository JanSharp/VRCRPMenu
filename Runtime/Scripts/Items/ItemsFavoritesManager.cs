using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    public enum ItemsFavoritesEventType
    {
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnItemFavoriteAdded,
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnItemFavoriteRemoved,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ItemsFavoritesEventAttribute : CustomRaisedEventBaseAttribute
    {
        /// <summary>
        /// <para>The method this attribute gets applied to must be public.</para>
        /// <para>The name of the function this attribute is applied to must have the exact same name as the
        /// name of the <paramref name="eventType"/>.</para>
        /// <para>Event registration is performed at OnBuild, which is to say that scripts with these kinds of
        /// event handlers must exist in the scene at build time, any runtime instantiated objects with these
        /// scripts on them will not receive these events.</para>
        /// <para>Disabled scripts still receive events.</para>
        /// </summary>
        /// <param name="eventType">The event to register this function as a listener to.</param>
        public ItemsFavoritesEventAttribute(ItemsFavoritesEventType eventType)
            : base((int)eventType)
        { }
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(ItemsFavoritesEventAttribute), typeof(ItemsFavoritesEventType))]
    [SingletonScript("cc557cc25e37164d491e0d6de003bc23")] // Runtime/Prefabs/Managers/ItemsFavoritesManager.prefab
    public class ItemsFavoritesManager : EntitiesFavoritesManager
    {
        [HideInInspector][SerializeField][SingletonReference] private ItemsPageManagerAPI itemsPageManager;

        protected override DataDictionary RelevantPrototypeNamesLut => itemsPageManager.ItemPrototypeNamesLut;

        protected override uint[] GetImportedFavoriteEntityIds(RPPlayerData player) => player.importedFavoriteItemIds;
        protected override void SetImportedFavoriteEntityIds(RPPlayerData player, uint[] importedIds) => player.importedFavoriteItemIds = importedIds;
        protected override DataDictionary GetFavoriteEntityIdsLut(RPPlayerData player) => player.favoriteItemIdsLut;
        protected override EntityPrototype[] GetFavoriteEntities(RPPlayerData player) => player.favoriteItems;
        protected override void SetFavoriteEntities(RPPlayerData player, EntityPrototype[] favorites) => player.favoriteItems = favorites;
        protected override int GetFavoriteEntitiesCount(RPPlayerData player) => player.favoriteItemsCount;
        protected override void SetFavoriteEntitiesCount(RPPlayerData player, int favoritesCount) => player.favoriteItemsCount = favoritesCount;

        protected override bool GetFavoritesAreIncluded(PlayersBackendImportExportOptions options) => options.includeFavoriteItems;

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onItemFavoriteAddedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onItemFavoriteRemovedListeners;

        protected override UdonSharpBehaviour[] OnFavoriteAddedListeners
        {
            get => onItemFavoriteAddedListeners;
            set => onItemFavoriteAddedListeners = value;
        }
        protected override UdonSharpBehaviour[] OnFavoriteRemovedListeners
        {
            get => onItemFavoriteRemovedListeners;
            set => onItemFavoriteRemovedListeners = value;
        }

        protected override string OnFavoriteAddedEventName => nameof(ItemsFavoritesEventType.OnItemFavoriteAdded);
        protected override string OnFavoriteRemovedEventName => nameof(ItemsFavoritesEventType.OnItemFavoriteRemoved);
    }
}
