using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CustomLocationLoadPopup : DynamicDataLoadPopup
    {
        [HideInInspector][SerializeField][SingletonReference] private CustomLocationManager customLocationManager;
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        protected override DynamicDataManager GetDynamicDataManager() => customLocationManager;

        protected override void LoadDynamicData(DynamicData data)
        {
            CustomLocation location = (CustomLocation)data;
            teleportManager.TeleportTo(location.position, location.rotation, recordUndo: false);
        }

        [CustomLocationEvent(CustomLocationEventType.OnCustomLocationAdded)]
        public void OnCustomLocationAdded()
        {
            OnDynamicDataAdded(customLocationManager.CustomLocationForEvent);
        }

        [CustomLocationEvent(CustomLocationEventType.OnCustomLocationOverwritten)]
        public void OnCustomLocationOverwritten()
        {
            OnDynamicDataOverwritten(customLocationManager.CustomLocationForEvent, customLocationManager.OverwrittenCustomLocationForEvent);
        }

        [CustomLocationEvent(CustomLocationEventType.OnCustomLocationDeleted)]
        public void OnCustomLocationDeleted()
        {
            OnDynamicDataDeleted(customLocationManager.CustomLocationForEvent);
        }

        // protected override string GetDynamicDataLabel(DynamicData data)
        // {
        //     CustomLocation location = (CustomLocation)data;
        //     return $"{base.GetDynamicDataLabel(data)} ({group.selectedPlayers.Length})";
        // }
    }
}
