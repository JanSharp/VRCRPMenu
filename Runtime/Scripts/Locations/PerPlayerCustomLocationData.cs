using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PerPlayerCustomLocationData : PerPlayerDynamicData
    {
        public override string PlayerDataInternalName => "jansharp.rp-menu-custom-locations";
        public override string PlayerDataDisplayName => "Custom Locations";

        [HideInInspector][SerializeField][SingletonReference] protected CustomLocationManager customLocationManager;
        public override DynamicDataManager DataManager => customLocationManager;

        public override string DynamicDataClassName => nameof(CustomLocation);
    }
}
