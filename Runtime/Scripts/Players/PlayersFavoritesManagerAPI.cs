using UdonSharp;

namespace JanSharp
{
    public enum PlayersFavoritesEventType
    {
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerFavoriteAdded,
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerFavoriteRemoved,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PlayersFavoritesEventAttribute : CustomRaisedEventBaseAttribute
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
        public PlayersFavoritesEventAttribute(PlayersFavoritesEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("07dbdb7a907619226b330ff0680d5773")] // Runtime/Prefabs/Managers/PlayersFavoritesManager.prefab
    public abstract class PlayersFavoritesManagerAPI : UdonSharpBehaviour
    {
        public abstract void SendAddFavoritePlayerIA(RPPlayerData source, RPPlayerData target);
        public abstract void SendRemoveFavoritePlayerIA(RPPlayerData source, RPPlayerData target);

        public abstract RPPlayerData SourcePlayerForEvent { get; }
        public abstract RPPlayerData TargetPlayerForEvent { get; }
    }
}
