using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    public enum PlayerSummonEventType
    {
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalPlayerSummonEnqueued,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalPlayerSummoned,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PlayerSummonEventAttribute : CustomRaisedEventBaseAttribute
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
        public PlayerSummonEventAttribute(PlayerSummonEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("df8ec3e261a2a384197cc011d7d2e63f")] // Runtime/Prefabs/Managers/PlayerSummonManager.prefab
    public abstract class PlayerSummonManagerAPI : UdonSharpBehaviour
    {
        public abstract float SummonDelay { get; }
        public abstract Vector3 SummonTargetPosition { get; }
        public abstract Quaternion SummonTargetRotation { get; }
        public abstract int LocalPlayerSummonDelayedEventCount { get; }
        public abstract float SummonEnqueueTime { get; }
        public abstract float SummonTargetTime { get; }

        public abstract PlayerSummonIndicatorGroup ShowIndicatorsInACircle(Vector3 position, Quaternion rotation, int indictorCount);
        /// <summary>
        /// </summary>
        /// <param name="locations"></param>
        /// <param name="players">Only reads the amount of players matching the
        /// <see cref="PlayerSummonIndicatorGroup.indicators"/> length. Skipped players due to
        /// <paramref name="playersToExclude"/> are not counted.</param>
        /// <param name="playersToExclude">
        /// <para><see cref="CorePlayerData"/> player => <see langword="true"/></para>
        /// <para>Can be <see langword="null"/>.</para>
        /// </param>
        /// <returns></returns>
        public abstract void SummonPlayers(PlayerSummonIndicatorGroup locations, CorePlayerData[] players, DataDictionary playersToExclude);
    }
}
