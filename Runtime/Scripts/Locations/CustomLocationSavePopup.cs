using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CustomLocationSavePopup : DynamicDataSavePopup
    {
        [HideInInspector][SerializeField][SingletonReference] private CustomLocationManager customLocationManager;
        protected override DynamicDataManager GetDynamicDataManager() => customLocationManager;

        private VRCPlayerApi localPlayer;

        public override void OnMenuManagerStart()
        {
            localPlayer = Networking.LocalPlayer;
            base.OnMenuManagerStart();
        }

        protected override void PopulateDataForAdd(DynamicData data)
        {
            CustomLocation location = (CustomLocation)data;
            location.position = localPlayer.GetPosition();
            location.rotation = localPlayer.GetRotation();
        }

        protected override void PopulateDataForOverwrite(DynamicData data, DynamicData toOverwrite)
        {
            PopulateDataForAdd(data);
        }

        protected override void PopulateDataForUndoOverwrite(DynamicData data, DynamicData toRestore)
        {
            CustomLocation location = (CustomLocation)data;
            CustomLocation locationToRestore = (CustomLocation)toRestore;
            location.position = locationToRestore.position;
            location.rotation = locationToRestore.rotation;
        }

        protected override string DefaultBaseDataName => "Location";

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

        [CustomLocationEvent(CustomLocationEventType.OnCustomLocationUndoOverwriteStackChanged)]
        public void OnCustomLocationUndoOverwriteStackChanged()
        {
            OnDynamicDataUndoOverwriteStackChanged();
        }

        // protected override string GetDynamicDataLabel(DynamicData data)
        // {
        //     CustomLocation location = (CustomLocation)data;
        //     return $"{base.GetDynamicDataLabel(data)} ({group.selectedPlayers.Length})";
        // }
    }
}
