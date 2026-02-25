using UdonSharp;

namespace JanSharp
{
    public enum MenuSettingsEventType
    {
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyUISoundsEnabledSettingChanged,
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyUISoundsVolumeSettingChanged,
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyDefaultPageSettingChanged,
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyOpenCloseKeyBindSettingChanged,
        /// <summary>
        /// <para>Unlike several other systems, this does get raised for imports.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalLatencyMenuPositionSettingChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class MenuSettingsEventAttribute : CustomRaisedEventBaseAttribute
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
        public MenuSettingsEventAttribute(MenuSettingsEventType eventType)
            : base((int)eventType)
        { }
    }

    /// <summary>
    /// <para>Values are guaranteed to never change with updates, making them save for serialization.</para>
    /// </summary>
    public enum RPMenuDefaultPageType : byte
    {
        Home = 0,
        LastOpen = 1,
    }

    [SingletonScript("3218dea9483cea14a8af077049ea7e6c")] // Runtime/Prefabs/Managers/MenuSettingsManager.prefab
    public abstract class MenuSettingsManagerAPI : LockstepGameState
    {
        public abstract bool InitialUISoundsEnabled { get; }
        public abstract float InitialUISoundsVolume { get; }
        public abstract RPMenuDefaultPageType InitialDefaultPage { get; }
        public abstract MenuOpenCloseKeyBind InitialMenuOpenCloseKeyBind { get; }
        public abstract MenuPositionType InitialMenuPosition { get; }

        public abstract bool LatencyUISoundsEnabled { get; }
        public abstract float LatencyUISoundsVolume { get; }
        public abstract RPMenuDefaultPageType LatencyDefaultPage { get; }
        public abstract MenuOpenCloseKeyBind LatencyMenuOpenCloseKeyBind { get; }
        public abstract MenuPositionType LatencyMenuPosition { get; }

        public abstract void SendSetUISoundsEnabledIA(PerPlayerMenuSettings settings, bool enabled);
        /// <summary>
        /// <para>Only usable to set the local player settings.</para>
        /// </summary>
        /// <param name="volume"></param>
        public abstract void SendSetUISoundsVolumeIARateLimited(float volume);
        public abstract void SendSetUISoundsVolumeIA(PerPlayerMenuSettings settings, float volume);
        public abstract void SendSetDefaultPageIA(PerPlayerMenuSettings settings, RPMenuDefaultPageType defaultPage);
        public abstract void SendSetOpenCloseKeyBindIA(PerPlayerMenuSettings settings, MenuOpenCloseKeyBind keyBind);
        public abstract void SendSetMenuPositionIA(PerPlayerMenuSettings settings, MenuPositionType menuPosition);

        /// <summary>
        /// <para>Used in exports.</para>
        /// </summary>
        public abstract MenuSettingsImportExportOptions ExportOptions { get; }
        /// <summary>
        /// <para>Used in imports.</para>
        /// </summary>
        public abstract MenuSettingsImportExportOptions ImportOptions { get; }
        /// <summary>
        /// <para>Used in imports.</para>
        /// </summary>
        public abstract MenuSettingsImportExportOptions OptionsFromExport { get; }

        public abstract PerPlayerMenuSettings LocalPlayerSettings { get; }
        public abstract PerPlayerMenuSettings SendingPerPlayerMenuSettings { get; }
        public abstract PerPlayerMenuSettings GetPerPlayerMenuSettings(CorePlayerData core);
        public abstract void WritePerPlayerMenuSettingsRef(PerPlayerMenuSettings settings);
        public abstract PerPlayerMenuSettings ReadPerPlayerMenuSettingsRef();
    }
}
