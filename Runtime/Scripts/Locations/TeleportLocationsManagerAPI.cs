using UdonSharp;

namespace JanSharp
{
    public enum TeleportLocationsEventType
    {
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnLocationBecameShown,
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        OnLocationBecameHidden,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class TeleportLocationsEventAttribute : CustomRaisedEventBaseAttribute
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
        public TeleportLocationsEventAttribute(TeleportLocationsEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("25360d627f06f7723b3ba4fa3aa03516")] // Runtime/Prefabs/Managers/TeleportLocationsManager.prefab
    public abstract class TeleportLocationsManagerAPI : UdonSharpBehaviour
    {
        public abstract TeleportLocation[] Locations { get; }
        public abstract int LocationsCount { get; }

        public abstract TeleportLocation[] ShownLocations { get; }
        public abstract int ShownLocationsCount { get; }

        public abstract void LocationBecomeShown(TeleportLocation location);
        public abstract void LocationBecameHidden(TeleportLocation location);

        public abstract TeleportLocation TeleportLocationForEvent { get; }
    }
}
