using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    public enum ObjectsFavoritesEventType
    {
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnObjectFavoriteAdded,
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnObjectFavoriteRemoved,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ObjectsFavoritesEventAttribute : CustomRaisedEventBaseAttribute
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
        public ObjectsFavoritesEventAttribute(ObjectsFavoritesEventType eventType)
            : base((int)eventType)
        { }
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(ObjectsFavoritesEventAttribute), typeof(ObjectsFavoritesEventType))]
    [SingletonScript("60f2827617c1ce90f9f3163bc772ccff")] // Runtime/Prefabs/Managers/ObjectsFavoritesManager.prefab
    public class ObjectsFavoritesManager : EntitiesFavoritesManager
    {
        [HideInInspector][SerializeField][SingletonReference] private ObjectsPageManagerAPI objectsPageManager;

        protected override DataDictionary RelevantPrototypeNamesLut => objectsPageManager.ObjectPrototypeNamesLut;

        protected override uint[] GetImportedFavoriteEntityIds(RPPlayerData player) => player.importedFavoriteObjectIds;
        protected override void SetImportedFavoriteEntityIds(RPPlayerData player, uint[] importedIds) => player.importedFavoriteObjectIds = importedIds;
        protected override DataDictionary GetFavoriteEntityIdsLut(RPPlayerData player) => player.favoriteObjectIdsLut;
        protected override EntityPrototype[] GetFavoriteEntities(RPPlayerData player) => player.favoriteObjects;
        protected override void SetFavoriteEntities(RPPlayerData player, EntityPrototype[] favorites) => player.favoriteObjects = favorites;
        protected override int GetFavoriteEntitiesCount(RPPlayerData player) => player.favoriteObjectsCount;
        protected override void SetFavoriteEntitiesCount(RPPlayerData player, int favoritesCount) => player.favoriteObjectsCount = favoritesCount;

        protected override bool GetFavoritesAreIncluded(PlayersBackendImportExportOptions options) => options.includeFavoriteObjects;

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onObjectFavoriteAddedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onObjectFavoriteRemovedListeners;

        protected override UdonSharpBehaviour[] OnFavoriteAddedListeners
        {
            get => onObjectFavoriteAddedListeners;
            set => onObjectFavoriteAddedListeners = value;
        }
        protected override UdonSharpBehaviour[] OnFavoriteRemovedListeners
        {
            get => onObjectFavoriteRemovedListeners;
            set => onObjectFavoriteRemovedListeners = value;
        }

        protected override string OnFavoriteAddedEventName => nameof(ObjectsFavoritesEventType.OnObjectFavoriteAdded);
        protected override string OnFavoriteRemovedEventName => nameof(ObjectsFavoritesEventType.OnObjectFavoriteRemoved);
    }
}
