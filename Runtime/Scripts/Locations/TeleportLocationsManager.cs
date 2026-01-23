using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(TeleportLocationsEventAttribute), typeof(TeleportLocationsEventType))]
    public class TeleportLocationsManager : TeleportLocationsManagerAPI
    {
        /// <summary>
        /// <para>Contains editor only locations, can contain <see langword="null"/> at runtime.</para>
        /// </summary>
        [SerializeField] private TeleportLocation[] allLocations;
        [SerializeField] private string[] categoryNames;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public TeleportLocation[] AllLocations => allLocations;
        public string[] CategoryNames => categoryNames;
#endif

        [SerializeField] private TeleportLocation[] locations;
        public override TeleportLocation[] Locations => locations;
        public override int LocationsCount => locations.Length;

        private TeleportLocation[] shownLocations;
        private int shownLocationsCount;
        public override TeleportLocation[] ShownLocations => shownLocations;
        public override int ShownLocationsCount => shownLocationsCount;

        private void Start()
        {
            shownLocations = new TeleportLocation[locations.Length];
        }

        public override void LocationBecomeShown(TeleportLocation location)
        {
            location.indexInShownList = shownLocationsCount;
            shownLocations[shownLocationsCount++] = location;
            RaiseOnLocationBecameShown(location);
        }

        public override void LocationBecameHidden(TeleportLocation location)
        {
            int index = location.indexInShownList;
            if ((--shownLocationsCount) == index)
            {
                RaiseOnLocationBecameHidden(location);
                return;
            }
            TeleportLocation top = shownLocations[index];
            top.indexInShownList = index;
            shownLocations[index] = top;
            RaiseOnLocationBecameHidden(location);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocationBecameShownListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocationBecameHiddenListeners;

        public TeleportLocation teleportLocationForEvent;
        public override TeleportLocation TeleportLocationForEvent => teleportLocationForEvent;

        private void RaiseOnLocationBecameShown(TeleportLocation teleportLocationForEvent)
        {
            this.teleportLocationForEvent = teleportLocationForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocationBecameShownListeners, nameof(TeleportLocationsEventType.OnLocationBecameShown));
            this.teleportLocationForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnLocationBecameHidden(TeleportLocation teleportLocationForEvent)
        {
            this.teleportLocationForEvent = teleportLocationForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocationBecameHiddenListeners, nameof(TeleportLocationsEventType.OnLocationBecameHidden));
            this.teleportLocationForEvent = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
