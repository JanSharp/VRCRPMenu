using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeleportLocation : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private TeleportLocationsManagerAPI teleportLocationsManager;

        [SerializeField] private string displayName;
        [SerializeField] private string categoryName;
        [SerializeField] private int order;
        public string DisplayName => displayName;
        public string CategoryName => categoryName;
        public int Order => order;

        [System.NonSerialized] public int indexInShownList;

        public WhenConditionsAreMetType whenConditionsAreMet;

        public bool[] logicalAnds;
        public bool[] inverts;
        [SerializeField] private string[] assetGuids;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public string[] AssetGuids => assetGuids;
#endif
        public PermissionDefinition[] permissionDefs;

        private bool locationShouldBeShown;
        public bool LocationShouldBeShown
        {
            get => locationShouldBeShown;
            private set
            {
                if (locationShouldBeShown == value)
                    return;
                locationShouldBeShown = value;
                if (value)
                    teleportLocationsManager.LocationBecomeShown(this);
                else
                    teleportLocationsManager.LocationBecameHidden(this);
            }
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            bool conditionsMatching = PermissionsUtil.ResolveConditionsList(logicalAnds, inverts, permissionDefs);
            LocationShouldBeShown = (whenConditionsAreMet == WhenConditionsAreMetType.Show) == conditionsMatching;
        }
    }
}
