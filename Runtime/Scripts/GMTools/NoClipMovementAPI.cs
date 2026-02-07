using UdonSharp;

namespace JanSharp
{
    /// <summary>
    /// <para>Only applies when not near a collider.</para>
    /// </summary>
    public enum NoClipModeWhileStill
    {
        /// <summary>
        /// <para>Stay in a floating animation while not moving.</para>
        /// </summary>
        Velocity,
        /// <summary>
        /// <para>Stand in the air as though there was ground to walk on.</para>
        /// </summary>
        FakeGround,
    }

    /// <summary>
    /// <para>Also applies when standing still but being near a collider.</para>
    /// </summary>
    public enum NoClipModeWhileMoving
    {
        /// <summary>
        /// <para>Collides, not actually no clip.</para>
        /// </summary>
        Velocity,
        /// <summary>
        /// <para>Velocity while not near colliders, otherwise teleport.</para>
        /// </summary>
        Combo,
        /// <summary>
        /// <para>Always use teleport.</para>
        /// </summary>
        Teleport,
    }

    public enum NoClipVerticalMovementType
    {
        None,
        HeadLocalSpace,
        WorldSpace,
    }

    public enum NoClipMovementEventType
    {
        /// <summary>
        /// <para>Only raised if the value is actually different.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnIsNoClipActiveChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NoClipMovementEventAttribute : CustomRaisedEventBaseAttribute
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
        public NoClipMovementEventAttribute(NoClipMovementEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("a208660966802b7a19b48fcd51d32afa")] // Runtime/Prefabs/Managers/NoClipMovement.prefab
    public abstract class NoClipMovementAPI : UdonSharpBehaviour
    {
        /// <summary>
        /// <para>Meters per second.</para>
        /// </summary>
        public abstract float Speed { get; set; }
        public abstract NoClipModeWhileStill ModeWhileStill { get; set; }
        public abstract NoClipModeWhileMoving ModeWhileMoving { get; set; }
        public abstract NoClipVerticalMovementType VerticalMovement { get; set; }
        /// <summary>
        /// <para>How many seconds it take to go from 0% speed to 100% speed.</para>
        /// </summary>
        public abstract float InputSmoothingDuration { get; set; }
        public abstract bool SetVelocityToZeroWhileTeleporting { get; set; }
        public abstract bool IsNoClipActive { get; set; }
        public abstract void IncrementAvoidTeleporting();
        public abstract void DecrementAvoidTeleporting();
    }
}
