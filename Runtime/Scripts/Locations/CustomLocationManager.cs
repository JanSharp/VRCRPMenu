using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    public enum CustomLocationEventType
    {
        OnCustomLocationAdded,
        OnCustomLocationOverwritten,
        OnCustomLocationDeleted,
        OnCustomLocationUndoOverwriteStackChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class CustomLocationEventAttribute : CustomRaisedEventBaseAttribute
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
        public CustomLocationEventAttribute(CustomLocationEventType eventType)
            : base((int)eventType)
        { }
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [LockstepGameStateDependency(typeof(CustomLocationOptionsGS))]
    [SingletonScript("e2bd3d3e653c7ff52ad3c01bae45db7a")] // Runtime/Prefabs/Managers/CustomLocationManager.prefab
    [CustomRaisedEventsDispatcher(typeof(CustomLocationEventAttribute), typeof(CustomLocationEventType))]
    public class CustomLocationManager : DynamicDataManager
    {
        public override string GameStateInternalName => "jansharp.rp-menu-custom-locations";
        public override string GameStateDisplayName => "Custom Locations";

        public override string DynamicDataClassName => nameof(CustomLocation);
        public override string PerPlayerDataClassName => nameof(PerPlayerCustomLocationData);

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onCustomLocationAddedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onCustomLocationOverwrittenListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onCustomLocationDeletedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onCustomLocationUndoOverwriteStackChangedListeners;

        private CorePlayerData playerDataForEvent;
        public CorePlayerData PlayerDataForEvent => playerDataForEvent;

        private CustomLocation customLocationForEvent;
        public CustomLocation CustomLocationForEvent => customLocationForEvent;

        private CustomLocation overwrittenCustomLocationForEvent;
        public CustomLocation OverwrittenCustomLocationForEvent => overwrittenCustomLocationForEvent;

        protected override void RaiseOnDataAdded(DynamicData data)
        {
            customLocationForEvent = (CustomLocation)data;
            CustomRaisedEvents.Raise(ref onCustomLocationAddedListeners, nameof(CustomLocationEventType.OnCustomLocationAdded));
            customLocationForEvent = null; // To prevent misuse of the API.
        }

        protected override void RaiseOnDataOverwritten(DynamicData data, DynamicData overwrittenData)
        {
            customLocationForEvent = (CustomLocation)data;
            overwrittenCustomLocationForEvent = (CustomLocation)overwrittenData;
            CustomRaisedEvents.Raise(ref onCustomLocationOverwrittenListeners, nameof(CustomLocationEventType.OnCustomLocationOverwritten));
            customLocationForEvent = null; // To prevent misuse of the API.
            overwrittenCustomLocationForEvent = null; // To prevent misuse of the API.
        }

        protected override void RaiseOnDataDeleted(DynamicData data)
        {
            customLocationForEvent = (CustomLocation)data;
            CustomRaisedEvents.Raise(ref onCustomLocationDeletedListeners, nameof(CustomLocationEventType.OnCustomLocationDeleted));
            customLocationForEvent = null; // To prevent misuse of the API.
        }

        protected override void RaiseOnOverwriteUndoStackChanged()
        {
            CustomRaisedEvents.Raise(ref onCustomLocationUndoOverwriteStackChangedListeners, nameof(CustomLocationEventType.OnCustomLocationUndoOverwriteStackChanged));
        }

        #endregion
    }
}
