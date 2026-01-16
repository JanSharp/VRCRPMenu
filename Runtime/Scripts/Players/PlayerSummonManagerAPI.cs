using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [SingletonScript("df8ec3e261a2a384197cc011d7d2e63f")] // Runtime/Prefabs/Managers/PlayerSummonManager.prefab
    public abstract class PlayerSummonManagerAPI : UdonSharpBehaviour
    {
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
