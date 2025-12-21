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
        public abstract void SendCreateIA(GMRequestType requestType);
        public abstract void SendSetRequestTypeIA(GMRequest request, GMRequestType requestType);
        public abstract void SendMarkReadIA(GMRequest request);
        public abstract void SendMarkUnreadIA(GMRequest request);
        public abstract void SendDeleteIA(GMRequest request);

        public abstract GMRequest RequestForEvent { get; }
    }
}
