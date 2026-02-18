using UdonSharp;

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

    [SingletonScript("cc557cc25e37164d491e0d6de003bc23")] // Runtime/Prefabs/Managers/ItemsFavoritesManager.prefab
    public abstract class ItemsFavoritesManagerAPI : UdonSharpBehaviour
    {
        public abstract void SendAddFavoriteItemIA(RPPlayerData player, EntityPrototype prototype);
        public abstract void SendRemoveFavoriteItemIA(RPPlayerData player, EntityPrototype prototype);

        public abstract RPPlayerData PlayerForEvent { get; }
        public abstract EntityPrototype EntityPrototypeForEvent { get; }
    }
}
