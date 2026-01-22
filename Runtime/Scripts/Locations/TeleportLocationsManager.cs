using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeleportLocationsManager : TeleportLocationsManagerAPI
    {
        /// <summary>
        /// <para>Contains editor only locations, can contain <see langword="null"/> at runtime.</para>
        /// </summary>
        [SerializeField] private TeleportLocation[] locations;
        [SerializeField] private string[] categoryNames;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public TeleportLocation[] Locations => locations;
        public string[] CategoryNames => categoryNames;
#endif
    }
}
