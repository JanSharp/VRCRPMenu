using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(MenuSettingsEventAttribute), typeof(MenuSettingsEventType))]
    public class MenuSettingsManager : MenuSettingsManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private MenuInputHandler menuInputHandler;

        private int perPlayerMenuSettingsIndex;

        [SerializeField] private bool initialUISoundsEnabled = true;
        [Range(0f, 1f)]
        [SerializeField] private float initialUISoundsVolume = 0.5f;
        [SerializeField] private RPMenuDefaultPageType initialDefaultPage = RPMenuDefaultPageType.Home;
        private MenuOpenCloseKeyBind initialMenuOpenCloseKeyBind;
        private MenuPositionType initialMenuPosition;
        public override bool InitialUISoundsEnabled => initialUISoundsEnabled;
        public override float InitialUISoundsVolume => initialUISoundsVolume;
        public override RPMenuDefaultPageType InitialDefaultPage => initialDefaultPage;
        public override MenuOpenCloseKeyBind InitialMenuOpenCloseKeyBind => initialMenuOpenCloseKeyBind;
        public override MenuPositionType InitialMenuPosition => initialMenuPosition;

        #region LatencyState
        private DataDictionary latencyHiddenUniqueIds = new DataDictionary();
        private bool latencyUISoundsEnabled;
        private float latencyUISoundsVolume;
        private RPMenuDefaultPageType latencyDefaultPage;
        public override bool LatencyUISoundsEnabled => latencyUISoundsEnabled;
        public override float LatencyUISoundsVolume => latencyUISoundsVolume;
        public override RPMenuDefaultPageType LatencyDefaultPage => latencyDefaultPage;
        public override MenuOpenCloseKeyBind LatencyMenuOpenCloseKeyBind => menuInputHandler.keyBind;
        public override MenuPositionType LatencyMenuPosition => menuInputHandler.MenuPosition;
        #endregion

        private void Start()
        {
            initialMenuOpenCloseKeyBind = menuInputHandler.keyBind;
            initialMenuPosition = menuInputHandler.MenuPosition;
        }

        [PlayerDataEvent(PlayerDataEventType.OnRegisterCustomPlayerData)]
        public void OnRegisterCustomPlayerData()
        {
            playerDataManager.RegisterCustomPlayerData<PerPlayerMenuSettings>(nameof(PerPlayerMenuSettings));
        }

        [PlayerDataEvent(PlayerDataEventType.OnAllCustomPlayerDataRegistered)]
        public void OnAllCustomPlayerDataRegistered()
        {
            perPlayerMenuSettingsIndex = playerDataManager.GetPlayerDataClassNameIndex<PerPlayerMenuSettings>(nameof(PerPlayerMenuSettings));
        }

        /// <summary>
        /// <para>Internal api.</para>
        /// <para>Effectively gets called inside of <see cref="LockstepEventType.OnInit"/> and game state
        /// deserialization, in both cases <paramref name="suppressEvents"/> is <see langword="true"/>,
        /// therefore also making this the latency state initialization function.</para>
        /// </summary>
        /// <param name="localSettings"></param>
        /// <param name="suppressEvents"></param>
        public void ResetLatencyStateToGameState(PerPlayerMenuSettings localSettings, bool suppressEvents)
        {
            latencyHiddenUniqueIds.Clear();
            if (suppressEvents)
            {
                latencyUISoundsEnabled = localSettings.uiSoundsEnabled;
                latencyUISoundsVolume = localSettings.uiSoundsVolume;
                latencyDefaultPage = localSettings.defaultPage;
                menuInputHandler.keyBind = localSettings.menuOpenCloseKeyBind;
                menuInputHandler.MenuPosition = localSettings.menuPosition;
            }
            else
            {
                SetUISoundsEnabledInLS(localSettings.uiSoundsEnabled);
                SetUISoundsVolumeInLS(localSettings.uiSoundsVolume);
                SetDefaultPageInLS(localSettings.defaultPage);
                SetOpenCloseKeyBindInLS(localSettings.menuOpenCloseKeyBind);
                SetMenuPositionInLS(localSettings.menuPosition);
            }
        }

        private bool ShouldApplyReceivedIAToLatencyState(PerPlayerMenuSettings settings)
        {
            if (settings.core.isLocal)
                return false;
            if (latencyHiddenUniqueIds.Count == 0)
                return true;
            if (latencyHiddenUniqueIds.Remove(lockstep.SendingUniqueId))
                return false;
            latencyHiddenUniqueIds.Clear();
            ResetLatencyStateToGameState(settings, suppressEvents: false);
            return false;
        }

        #region InputActions

        public override void SendSetUISoundsEnabledIA(PerPlayerMenuSettings settings, bool enabled)
        {
            WritePerPlayerMenuSettingsRef(settings);
            lockstep.WriteFlags(enabled);
            ulong uniqueId = lockstep.SendInputAction(setUISoundsEnabledIAId);
            if (!settings.core.isLocal)
                return;
            latencyHiddenUniqueIds.Add(uniqueId, true);
            SetUISoundsEnabledInLS(enabled);
        }

        [HideInInspector][SerializeField] private uint setUISoundsEnabledIAId;
        [LockstepInputAction(nameof(setUISoundsEnabledIAId))]
        public void OnSetUISoundsEnabledIA()
        {
            PerPlayerMenuSettings settings = ReadPerPlayerMenuSettingsRef();
            lockstep.ReadFlags(out bool enabled);
            if (settings == null)
                return; // Can skip checking latencyHiddenUniqueIds, local settings are not going to be null.

            settings.uiSoundsEnabled = enabled;
            if (ShouldApplyReceivedIAToLatencyState(settings))
                SetUISoundsEnabledInLS(enabled);
        }

        private void SetUISoundsEnabledInLS(bool enabled)
        {
            if (latencyUISoundsEnabled == enabled)
                return;
            latencyUISoundsEnabled = enabled;
            RaiseOnLocalLatencyUISoundsEnabledSettingChanged();
        }

        public override void SendSetUISoundsVolumeIARateLimited(float volume)
        {
            SetUISoundsVolumeInLS(volume);
            if (setUISoundsVolumeIAIsQueued)
                return;
            setUISoundsVolumeIAIsQueued = true;
            SendCustomEventDelayedSeconds(nameof(SendSetUISoundsVolumeIADelayed), 0.5f);
        }

        private bool setUISoundsVolumeIAIsQueued = false;

        public void SendSetUISoundsVolumeIADelayed()
        {
            setUISoundsVolumeIAIsQueued = false;
            WritePerPlayerMenuSettingsRef(LocalPlayerSettings);
            lockstep.WriteFloat(LatencyUISoundsVolume);
            ulong uniqueId = lockstep.SendInputAction(setUISoundsVolumeIAId);
            latencyHiddenUniqueIds.Add(uniqueId, true);
        }

        public override void SendSetUISoundsVolumeIA(PerPlayerMenuSettings settings, float volume)
        {
            WritePerPlayerMenuSettingsRef(settings);
            lockstep.WriteFloat(volume);
            ulong uniqueId = lockstep.SendInputAction(setUISoundsVolumeIAId);
            if (!settings.core.isLocal)
                return;
            latencyHiddenUniqueIds.Add(uniqueId, true);
            SetUISoundsVolumeInLS(volume);
        }

        [HideInInspector][SerializeField] private uint setUISoundsVolumeIAId;
        [LockstepInputAction(nameof(setUISoundsVolumeIAId))]
        public void OnSetUISoundsVolumeIA()
        {
            PerPlayerMenuSettings settings = ReadPerPlayerMenuSettingsRef();
            float volume = lockstep.ReadFloat();
            if (settings == null)
                return; // Can skip checking latencyHiddenUniqueIds, local settings are not going to be null.

            settings.uiSoundsVolume = volume;
            if (ShouldApplyReceivedIAToLatencyState(settings))
                SetUISoundsVolumeInLS(volume);
        }

        private void SetUISoundsVolumeInLS(float volume)
        {
            if (latencyUISoundsVolume == volume)
                return;
            latencyUISoundsVolume = volume;
            RaiseOnLocalLatencyUISoundsVolumeSettingChanged();
        }

        public override void SendSetDefaultPageIA(PerPlayerMenuSettings settings, RPMenuDefaultPageType defaultPage)
        {
            WritePerPlayerMenuSettingsRef(settings);
            lockstep.WriteByte((byte)defaultPage);
            ulong uniqueId = lockstep.SendInputAction(setDefaultPageIAId);
            if (!settings.core.isLocal)
                return;
            latencyHiddenUniqueIds.Add(uniqueId, true);
            SetDefaultPageInLS(defaultPage);
        }

        [HideInInspector][SerializeField] private uint setDefaultPageIAId;
        [LockstepInputAction(nameof(setDefaultPageIAId))]
        public void OnSetDefaultPageIA()
        {
            PerPlayerMenuSettings settings = ReadPerPlayerMenuSettingsRef();
            RPMenuDefaultPageType defaultPage = (RPMenuDefaultPageType)lockstep.ReadByte();
            if (settings == null)
                return; // Can skip checking latencyHiddenUniqueIds, local settings are not going to be null.

            settings.defaultPage = defaultPage;
            if (ShouldApplyReceivedIAToLatencyState(settings))
                SetDefaultPageInLS(defaultPage);
        }

        private void SetDefaultPageInLS(RPMenuDefaultPageType defaultPage)
        {
            if (latencyDefaultPage == defaultPage)
                return;
            latencyDefaultPage = defaultPage;
            RaiseOnLocalLatencyDefaultPageSettingChanged();
        }

        public override void SendSetOpenCloseKeyBindIA(PerPlayerMenuSettings settings, MenuOpenCloseKeyBind keyBind)
        {
            WritePerPlayerMenuSettingsRef(settings);
            lockstep.WriteByte((byte)keyBind);
            ulong uniqueId = lockstep.SendInputAction(setOpenCloseKeyBindIAId);
            if (!settings.core.isLocal)
                return;
            latencyHiddenUniqueIds.Add(uniqueId, true);
            SetOpenCloseKeyBindInLS(keyBind);
        }

        [HideInInspector][SerializeField] private uint setOpenCloseKeyBindIAId;
        [LockstepInputAction(nameof(setOpenCloseKeyBindIAId))]
        public void OnSetOpenCloseKeyBindIA()
        {
            PerPlayerMenuSettings settings = ReadPerPlayerMenuSettingsRef();
            MenuOpenCloseKeyBind keyBind = (MenuOpenCloseKeyBind)lockstep.ReadByte();
            if (settings == null)
                return; // Can skip checking latencyHiddenUniqueIds, local settings are not going to be null.

            settings.menuOpenCloseKeyBind = keyBind;
            if (ShouldApplyReceivedIAToLatencyState(settings))
                SetOpenCloseKeyBindInLS(keyBind);
        }

        private void SetOpenCloseKeyBindInLS(MenuOpenCloseKeyBind keyBind)
        {
            if (menuInputHandler.keyBind == keyBind)
                return;
            menuInputHandler.keyBind = keyBind;
            RaiseOnLocalLatencyOpenCloseKeyBindSettingChanged();
        }

        public override void SendSetMenuPositionIA(PerPlayerMenuSettings settings, MenuPositionType menuPosition)
        {
            WritePerPlayerMenuSettingsRef(settings);
            lockstep.WriteByte((byte)menuPosition);
            ulong uniqueId = lockstep.SendInputAction(setMenuPositionIAId);
            if (!settings.core.isLocal)
                return;
            latencyHiddenUniqueIds.Add(uniqueId, true);
            SetMenuPositionInLS(menuPosition);
        }

        [HideInInspector][SerializeField] private uint setMenuPositionIAId;
        [LockstepInputAction(nameof(setMenuPositionIAId))]
        public void OnSetMenuPositionIA()
        {
            PerPlayerMenuSettings settings = ReadPerPlayerMenuSettingsRef();
            MenuPositionType menuPosition = (MenuPositionType)lockstep.ReadByte();
            if (settings == null)
                return; // Can skip checking latencyHiddenUniqueIds, local settings are not going to be null.

            settings.menuPosition = menuPosition;
            if (ShouldApplyReceivedIAToLatencyState(settings))
                SetMenuPositionInLS(menuPosition);
        }

        private void SetMenuPositionInLS(MenuPositionType menuPosition)
        {
            if (menuInputHandler.MenuPosition == menuPosition)
                return;
            menuInputHandler.MenuPosition = menuPosition;
            RaiseOnLocalLatencyMenuPositionSettingChanged();
        }

        #endregion

        #region Utilities

        public override PerPlayerMenuSettings LocalPlayerSettings => (PerPlayerMenuSettings)playerDataManager.LocalPlayerData.customPlayerData[perPlayerMenuSettingsIndex];

        public override PerPlayerMenuSettings SendingPerPlayerMenuSettings => (PerPlayerMenuSettings)playerDataManager.SendingPlayerData.customPlayerData[perPlayerMenuSettingsIndex];

        public override PerPlayerMenuSettings GetPerPlayerMenuSettings(CorePlayerData core) => (PerPlayerMenuSettings)core.customPlayerData[perPlayerMenuSettingsIndex];

        public override void WritePerPlayerMenuSettingsRef(PerPlayerMenuSettings settings)
        {
            playerDataManager.WriteCorePlayerDataRef(settings == null ? null : settings.core);
        }

        public override PerPlayerMenuSettings ReadPerPlayerMenuSettingsRef()
        {
            CorePlayerData core = playerDataManager.ReadCorePlayerDataRef();
            return core == null ? null : (PerPlayerMenuSettings)core.customPlayerData[perPlayerMenuSettingsIndex];
        }

        #endregion

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyUISoundsEnabledSettingChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyUISoundsVolumeSettingChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyDefaultPageSettingChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyOpenCloseKeyBindSettingChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyMenuPositionSettingChangedListeners;

        private void RaiseOnLocalLatencyUISoundsEnabledSettingChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyUISoundsEnabledSettingChangedListeners, nameof(MenuSettingsEventType.OnLocalLatencyUISoundsEnabledSettingChanged));
        }

        private void RaiseOnLocalLatencyUISoundsVolumeSettingChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyUISoundsVolumeSettingChangedListeners, nameof(MenuSettingsEventType.OnLocalLatencyUISoundsVolumeSettingChanged));
        }

        private void RaiseOnLocalLatencyDefaultPageSettingChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyDefaultPageSettingChangedListeners, nameof(MenuSettingsEventType.OnLocalLatencyDefaultPageSettingChanged));
        }

        private void RaiseOnLocalLatencyOpenCloseKeyBindSettingChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyOpenCloseKeyBindSettingChangedListeners, nameof(MenuSettingsEventType.OnLocalLatencyOpenCloseKeyBindSettingChanged));
        }

        private void RaiseOnLocalLatencyMenuPositionSettingChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyMenuPositionSettingChangedListeners, nameof(MenuSettingsEventType.OnLocalLatencyMenuPositionSettingChanged));
        }

        #endregion
    }
}
