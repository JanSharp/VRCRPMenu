using UdonSharp;

namespace JanSharp
{
    public enum PlayersBackendEventType
    {
        /// <summary>
        /// <para>Use <see cref="PlayersBackendManagerAPI.RPPlayerDataForEvent"/> to get the player data for
        /// which the <see cref="RPPlayerData.overriddenDisplayName"/> has been changed.</para>
        /// <para>Use <see cref="PlayersBackendManagerAPI.PreviousOverriddenDisplayName"/> to get the previous
        /// value of <see cref="RPPlayerData.overriddenDisplayName"/> before the change.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnRPPlayerDataOverriddenDisplayNameChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PlayersBackendEventAttribute : CustomRaisedEventBaseAttribute
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
        public PlayersBackendEventAttribute(PlayersBackendEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("000377df80d8968ff9358be09d3fa9e3")] // Runtime/Prefabs/PlayersBackendManager.prefab
    public abstract class PlayersBackendManagerAPI : UdonSharpBehaviour
    {
        public abstract void SendSetOverriddenDisplayNameIA(RPPlayerData rpPlayerData, string overriddenDisplayName);
        public abstract void SetOverriddenDisplayNameInGS(RPPlayerData rpPlayerData, string overriddenDisplayName);

        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract RPPlayerData RPPlayerDataForEvent { get; }
        /// <summary>
        /// <para>Usable inside of
        /// <see cref="PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract string PreviousOverriddenDisplayName { get; }
    }
}
