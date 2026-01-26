using UdonSharp;

namespace JanSharp
{
    public enum NoClipEventType
    {
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyNoClipEnabledChanged,
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyNoClipSpeedChanged,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnIsNoClipActiveChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NoClipEventAttribute : CustomRaisedEventBaseAttribute
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
        public NoClipEventAttribute(NoClipEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("21117a51120db00258006c8192882928")] // Runtime/Prefabs/Managers/NoClipManager.prefab
    public abstract class NoClipManagerAPI : UdonSharpBehaviour
    {
        public abstract bool InitialNoClipEnabled { get; }
        public abstract float InitialNoClipSpeed { get; }

        public abstract bool LatencyNoClipEnabled { get; }
        public abstract float LatencyNoClipSpeed { get; }

        public abstract bool IsNoClipActive { get; set; }

        public abstract void SendSetNoClipEnabledIA(NoClipPlayerData data, bool noClipEnabled);
        public abstract void SendSetNoClipSpeedIA(NoClipPlayerData data, float noClipSpeed);

        public abstract NoClipPlayerData LocalNoClipPlayerData { get; }
        public abstract NoClipPlayerData SendingNoClipPlayerData { get; }
        public abstract NoClipPlayerData GetNoClipPlayerData(CorePlayerData core);
        public abstract void WriteNoClipPlayerDataRef(NoClipPlayerData data);
        public abstract NoClipPlayerData ReadNoClipPlayerDataRef();
    }
}
