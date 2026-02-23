namespace JanSharp
{
    public enum VoiceRangeVisualizationType : byte
    {
        Static = 0,
        Pulse = 1,
        Blink = 2,
    }

    public enum VoiceRangeEventType
    {
        /// <summary>
        /// <para>Use <see cref="VoiceRangeManagerAPI.PlayerDataForEvent"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnVoiceRangeIndexChangedInLatency,
        /// <summary>
        /// <para>Use <see cref="VoiceRangeManagerAPI.PlayerDataForEvent"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnVoiceRangeIndexChanged,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalVoiceRangeIndexChangedInLatency,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalVoiceRangeIndexChanged,
        /// <summary>
        /// <para>Use <see cref="VoiceRangeManagerAPI.PlayerDataForEvent"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnInWorldSettingsChangedInLatency,
        /// <summary>
        /// <para>Use <see cref="VoiceRangeManagerAPI.PlayerDataForEvent"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnInWorldSettingsChanged,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalInWorldSettingsChangedInLatency,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalInWorldSettingsChanged,
        /// <summary>
        /// <para>Use <see cref="VoiceRangeManagerAPI.PlayerDataForEvent"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnInHUDSettingsChangedInLatency,
        /// <summary>
        /// <para>Use <see cref="VoiceRangeManagerAPI.PlayerDataForEvent"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnInHUDSettingsChanged,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalInHUDSettingsChangedInLatency,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalInHUDSettingsChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class VoiceRangeEventAttribute : CustomRaisedEventBaseAttribute
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
        public VoiceRangeEventAttribute(VoiceRangeEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("2c883c84a21e7a786a9cb7778e7a00fb")] // Runtime/Prefabs/Managers/VoiceRangeManager.prefab
    public abstract class VoiceRangeManagerAPI : LockstepGameState
    {
        /// <summary>
        /// <para>Initialized in <c>Start</c>, usable any time after that.</para>
        /// <para>Game sate safe.</para>
        /// </summary>
        public abstract int DefaultVoiceRangeIndex { get; }
        /// <summary>
        /// <para>Initialized in <c>Start</c>, usable any time after that.</para>
        /// <para>Game sate safe.</para>
        /// </summary>
        public abstract uint DefaultShowInWorldMask { get; }
        /// <summary>
        /// <para>Initialized in <c>Start</c>, usable any time after that.</para>
        /// <para>Game sate safe.</para>
        /// </summary>
        public abstract VoiceRangeVisualizationType DefaultWorldVisualType { get; }
        /// <summary>
        /// <para>Initialized in <c>Start</c>, usable any time after that.</para>
        /// <para>Game sate safe.</para>
        /// </summary>
        public abstract uint DefaultShowInHUDMask { get; }
        /// <summary>
        /// <para>Initialized in <c>Start</c>, usable any time after that.</para>
        /// <para>Game sate safe.</para>
        /// </summary>
        public abstract VoiceRangeVisualizationType DefaultHUDVisualType { get; }

        public abstract int VoiceRangeDefinitionCount { get; }
        public abstract VoiceRangeDefinition GetVoiceRangeDefinition(int index);
        public abstract VoiceRangeDefinition GetVoiceRangeDefinition(string internalName);

        public abstract VoiceRangePlayerData LocalPlayer { get; }

        public abstract void SendSetVoiceRangeIndexIA(VoiceRangePlayerData player, int voiceRangeIndex);
        public abstract void SendSetInWorldSettingsIA(VoiceRangePlayerData player, uint showMask, VoiceRangeVisualizationType visualType);
        public abstract void SendSetInHUDSettingsIA(VoiceRangePlayerData player, uint showMask, VoiceRangeVisualizationType visualType);

        public abstract VoiceRangePlayerData GetVoiceRangePlayerData(CorePlayerData core);
        public abstract void WriteVoiceRangePlayerDataRef(VoiceRangePlayerData voiceRangePlayerData);
        public abstract VoiceRangePlayerData ReadVoiceRangePlayerDataRef();

        public abstract VoiceRangePlayerData PlayerDataForEvent { get; }
    }
}
