namespace JanSharp
{
    public enum NoClipSettingsEventType
    {
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Only raised if the value is actually different.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyNoClipEnabledChanged,
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Only raised if the value is actually different.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyNoClipFlyingTypeChanged,
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Only raised if the value is actually different.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyNoClipSpeedChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NoClipSettingsEventAttribute : CustomRaisedEventBaseAttribute
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
        public NoClipSettingsEventAttribute(NoClipSettingsEventType eventType)
            : base((int)eventType)
        { }
    }

    /// <summary>
    /// <para>Values are guaranteed to never change with updates, making them safe for serialization.</para>
    /// </summary>
    public enum NoClipFlyingType : byte
    {
        Flying = 0,
        NoClip = 1,
    }

    [SingletonScript("21117a51120db00258006c8192882928")] // Runtime/Prefabs/Managers/NoClipManager.prefab
    public abstract class NoClipSettingsManagerAPI : LockstepGameState
    {
        public abstract bool InitialNoClipEnabled { get; }
        public abstract NoClipFlyingType InitialNoClipFlyingType { get; }
        /// <summary>
        /// <para>Meters per second.</para>
        /// </summary>
        public abstract float InitialNoClipSpeed { get; }

        public abstract bool LatencyNoClipEnabled { get; }
        public abstract NoClipFlyingType LatencyNoClipFlyingType { get; }
        /// <summary>
        /// <para>Meters per second.</para>
        /// </summary>
        public abstract float LatencyNoClipSpeed { get; }

        public abstract void SendSetNoClipEnabledIA(NoClipSettingsPlayerData data, bool noClipEnabled);
        public abstract void SendSetNoClipFlyingTypeIA(NoClipSettingsPlayerData data, NoClipFlyingType noClipFlyingType);
        public abstract void SendSetNoClipSpeedIA(NoClipSettingsPlayerData data, float noClipSpeed);

        /// <summary>
        /// <para>Used in exports.</para>
        /// </summary>
        public abstract NoClipImportExportOptions ExportOptions { get; }
        /// <summary>
        /// <para>Used in imports.</para>
        /// </summary>
        public abstract NoClipImportExportOptions ImportOptions { get; }
        /// <summary>
        /// <para>Used in imports.</para>
        /// </summary>
        public abstract NoClipImportExportOptions OptionsFromExport { get; }

        public abstract NoClipSettingsPlayerData LocalNoClipSettingsPlayerData { get; }
        public abstract NoClipSettingsPlayerData SendingNoClipSettingsPlayerData { get; }
        public abstract NoClipSettingsPlayerData GetNoClipSettingsPlayerData(CorePlayerData core);
        public abstract void WriteNoClipSettingsPlayerDataRef(NoClipSettingsPlayerData data);
        public abstract NoClipSettingsPlayerData ReadNoClipSettingsPlayerDataRef();
    }
}
