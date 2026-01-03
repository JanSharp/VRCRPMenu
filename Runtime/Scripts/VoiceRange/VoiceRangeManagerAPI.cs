using UdonSharp;

namespace JanSharp
{
    public enum VoiceRangeVisualizationType : byte
    {
        Static = 0,
        Pulse = 1,
        Blink = 2,
        Default = Pulse,
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
        public abstract uint DefaultShowInHUDMask { get; }
    }
}
