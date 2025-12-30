namespace JanSharp
{
    public enum GMRequestsEventType
    {
        /// <summary>
        /// <para>Use <see cref="GMRequestsManagerAPI.RequestForEvent"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestCreatedInLatency,
        /// <summary>
        /// <para>Use <see cref="GMRequestsManagerAPI.RequestForEvent"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnGMRequestCreated,
        /// <summary>
        /// <para>Use <see cref="GMRequestsManagerAPI.RequestForEvent"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestChangedInLatency,
        /// <summary>
        /// <para>Use <see cref="GMRequestsManagerAPI.RequestForEvent"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnGMRequestChanged,
        /// <summary>
        /// <para>Use <see cref="GMRequestsManagerAPI.RequestForEvent"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestDeletedInLatency,
        /// <summary>
        /// <para>Raised when a delete attempt was made, <see cref="OnGMRequestDeletedInLatency"/> got raised,
        /// but turns out the player did not have delete permission.</para>
        /// <para>Use <see cref="GMRequestsManagerAPI.RequestForEvent"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestUnDeletedInLatency,
        /// <summary>
        /// <para>Use <see cref="GMRequestsManagerAPI.RequestForEvent"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnGMRequestDeleted,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class GMRequestsEventAttribute : CustomRaisedEventBaseAttribute
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
        public GMRequestsEventAttribute(GMRequestsEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("244fcac5ffbfe1f3b8c588e7f8d9ee5b")] // Runtime/Prefabs/Managers/GMRequestsManager.prefab
    public abstract class GMRequestsManagerAPI : LockstepGameState
    {
        // As a general note, there is no way to get GMRequests which have been created in the latency state
        // but aren't in the game state yet.
        /// <summary>
        /// <para>Game state safe, including order.</para>
        /// </summary>
        public abstract GMRequest[] GMRequests { get; }
        /// <summary>
        /// <para>A direct reference to the internal array, which is an <see cref="ArrList"/>, which is to say
        /// that the <see cref="System.Array.Length"/> of this array cannot be trusted.</para>
        /// <para>It being an <see cref="ArrList"/> also implies that fetching this property and keeping a
        /// reference to the returned value can end up referring to a stale no longer used array in the
        /// future, if the arrays has been grown internally since fetching it.</para>
        /// <para>The actual amount of elements used of this array is defined via
        /// <see cref="GMRequestsCount"/>.</para>
        /// <para>Game state safe, including order.</para>
        /// </summary>
        public abstract GMRequest[] GMRequestsRaw { get; }
        public abstract GMRequest GetGMRequest(int index);
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract int GMRequestsCount { get; }

        public abstract void SendCreateIA(GMRequestType requestType);
        public abstract void SendSetRequestTypeIA(GMRequest request, GMRequestType requestType);
        public abstract void SendMarkReadIA(GMRequest request);
        public abstract void SendMarkUnreadIA(GMRequest request);
        public abstract void SendDeleteIA(GMRequest request);

        public abstract void WriteGMRequestRef(GMRequest request);
        public abstract GMRequest ReadGMRequestRef();
        public abstract GMRequest GetGMRequestById(uint id);

        public abstract GMRequest RequestForEvent { get; }
    }
}
