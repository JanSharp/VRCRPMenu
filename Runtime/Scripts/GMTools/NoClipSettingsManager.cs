using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [LockstepGameStateDependency(typeof(PlayerDataManagerAPI), SelfLoadsBeforeDependency = true)]
    [CustomRaisedEventsDispatcher(typeof(NoClipSettingsEventAttribute), typeof(NoClipSettingsEventType))]
    public class NoClipSettingsManager : NoClipSettingsManagerAPI
    {
        public override string GameStateInternalName => "jansharp.rp-menu-no-clip-settings";
        public override string GameStateDisplayName => "No Clip Settings";
        public override bool GameStateSupportsImportExport => true;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;
        [SerializeField] private NoClipImportExportOptionsUI exportUI;
        [SerializeField] private NoClipImportExportOptionsUI importUI;
        public override LockstepGameStateOptionsUI ExportUI => exportUI;
        public override LockstepGameStateOptionsUI ImportUI => importUI;

        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        public override NoClipImportExportOptions ExportOptions => (NoClipImportExportOptions)OptionsForCurrentExport;
        public override NoClipImportExportOptions ImportOptions => (NoClipImportExportOptions)OptionsForCurrentImport;
        private NoClipImportExportOptions optionsFromExport;
        public override NoClipImportExportOptions OptionsFromExport => optionsFromExport;

        private int noClipPlayerDataIndex;

        [SerializeField] private bool initialNoClipEnabled = false;
        [Min(0f)]
        [SerializeField] private float initialNoClipSpeed = 32f;
        public override bool InitialNoClipEnabled => initialNoClipEnabled;
        public override float InitialNoClipSpeed => initialNoClipSpeed;

        #region LatencyState
        private DataDictionary latencyHiddenUniqueIds = new DataDictionary();
        private bool latencyNoClipEnabled;
        private float latencyNoClipSpeed;
        public override bool LatencyNoClipEnabled => latencyNoClipEnabled;
        public override float LatencyNoClipSpeed => latencyNoClipSpeed;
        #endregion

        [PlayerDataEvent(PlayerDataEventType.OnRegisterCustomPlayerData)]
        public void OnRegisterCustomPlayerData()
        {
            playerDataManager.RegisterCustomPlayerData<NoClipSettingsPlayerData>(nameof(NoClipSettingsPlayerData));
        }

        [PlayerDataEvent(PlayerDataEventType.OnAllCustomPlayerDataRegistered)]
        public void OnAllCustomPlayerDataRegistered()
        {
            noClipPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<NoClipSettingsPlayerData>(nameof(NoClipSettingsPlayerData));
        }

        /// <summary>
        /// <para>Internal api.</para>
        /// <para>Effectively gets called inside of <see cref="LockstepEventType.OnInit"/> and game state
        /// deserialization, in both cases <paramref name="suppressEvents"/> is <see langword="true"/>,
        /// therefore also making this the latency state initialization function.</para>
        /// </summary>
        /// <param name="localData"></param>
        /// <param name="suppressEvents"></param>
        public void ResetLatencyStateToGameState(NoClipSettingsPlayerData localData, bool suppressEvents)
        {
            latencyHiddenUniqueIds.Clear();
            if (suppressEvents)
            {
                latencyNoClipEnabled = localData.noClipEnabled;
                latencyNoClipSpeed = localData.noClipSpeed;
            }
            else
            {
                SetNoClipEnabledInLS(localData.noClipEnabled);
                SetNoClipSpeedInLS(localData.noClipSpeed);
            }
        }

        private bool ShouldApplyReceivedIAToLatencyState(NoClipSettingsPlayerData settings)
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

        public override void SendSetNoClipEnabledIA(NoClipSettingsPlayerData data, bool noClipEnabled)
        {
            WriteNoClipSettingsPlayerDataRef(data);
            lockstep.WriteFlags(noClipEnabled);
            ulong uniqueId = lockstep.SendInputAction(setNoClipEnabledIAId);
            if (!data.core.isLocal)
                return;
            latencyHiddenUniqueIds.Add(uniqueId, true);
            SetNoClipEnabledInLS(noClipEnabled);
        }

        [HideInInspector][SerializeField] private uint setNoClipEnabledIAId;
        [LockstepInputAction(nameof(setNoClipEnabledIAId))]
        public void OnSetNoClipEnabledIA()
        {
            NoClipSettingsPlayerData data = ReadNoClipSettingsPlayerDataRef();
            lockstep.ReadFlags(out bool noClipEnabled);
            if (data == null)
                return; // Can skip checking latencyHiddenUniqueIds, local settings are not going to be null.

            data.noClipEnabled = noClipEnabled;
            if (ShouldApplyReceivedIAToLatencyState(data))
                SetNoClipEnabledInLS(noClipEnabled);
        }

        private void SetNoClipEnabledInLS(bool noClipEnabled)
        {
            if (latencyNoClipEnabled == noClipEnabled)
                return;
            latencyNoClipEnabled = noClipEnabled;
            RaiseOnLocalLatencyNoClipEnabledChanged();
        }

        public override void SendSetNoClipSpeedIA(NoClipSettingsPlayerData data, float noClipSpeed)
        {
            WriteNoClipSettingsPlayerDataRef(data);
            lockstep.WriteFloat(noClipSpeed);
            ulong uniqueId = lockstep.SendInputAction(setNoClipSpeedIAId);
            if (!data.core.isLocal)
                return;
            latencyHiddenUniqueIds.Add(uniqueId, true);
            SetNoClipSpeedInLS(noClipSpeed);
        }

        [HideInInspector][SerializeField] private uint setNoClipSpeedIAId;
        [LockstepInputAction(nameof(setNoClipSpeedIAId))]
        public void OnSetNoClipSpeedIA()
        {
            NoClipSettingsPlayerData data = ReadNoClipSettingsPlayerDataRef();
            float noClipSpeed = lockstep.ReadFloat();
            if (data == null)
                return; // Can skip checking latencyHiddenUniqueIds, local settings are not going to be null.

            data.noClipSpeed = noClipSpeed;
            if (ShouldApplyReceivedIAToLatencyState(data))
                SetNoClipSpeedInLS(noClipSpeed);
        }

        private void SetNoClipSpeedInLS(float noClipSpeed)
        {
            if (latencyNoClipSpeed == noClipSpeed)
                return;
            latencyNoClipSpeed = noClipSpeed;
            RaiseOnLocalLatencyNoClipSpeedChanged();
        }

        #endregion

        #region Utilities

        public override NoClipSettingsPlayerData LocalNoClipSettingsPlayerData => (NoClipSettingsPlayerData)playerDataManager.LocalPlayerData.customPlayerData[noClipPlayerDataIndex];

        public override NoClipSettingsPlayerData SendingNoClipSettingsPlayerData => (NoClipSettingsPlayerData)playerDataManager.SendingPlayerData.customPlayerData[noClipPlayerDataIndex];

        public override NoClipSettingsPlayerData GetNoClipSettingsPlayerData(CorePlayerData core) => (NoClipSettingsPlayerData)core.customPlayerData[noClipPlayerDataIndex];

        public override void WriteNoClipSettingsPlayerDataRef(NoClipSettingsPlayerData data)
        {
            playerDataManager.WriteCorePlayerDataRef(data == null ? null : data.core);
        }

        public override NoClipSettingsPlayerData ReadNoClipSettingsPlayerDataRef()
        {
            CorePlayerData core = playerDataManager.ReadCorePlayerDataRef();
            return core == null ? null : (NoClipSettingsPlayerData)core.customPlayerData[noClipPlayerDataIndex];
        }

        #endregion

        #region Serialization

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
            if (!isExport)
                return;
            lockstep.WriteCustomClass(exportOptions);
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
            if (!isImport)
                return null;
            optionsFromExport = (NoClipImportExportOptions)lockstep.ReadCustomClass(nameof(NoClipImportExportOptions));
            return null;
        }

        [LockstepEvent(LockstepEventType.OnImportFinished, Order = 1000)]
        public void OnImportFinished()
        {
            optionsFromExport = null;
        }

        #endregion

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyNoClipEnabledChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyNoClipSpeedChangedListeners;

        private void RaiseOnLocalLatencyNoClipEnabledChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyNoClipEnabledChangedListeners, nameof(NoClipSettingsEventType.OnLocalLatencyNoClipEnabledChanged));
        }

        private void RaiseOnLocalLatencyNoClipSpeedChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyNoClipSpeedChangedListeners, nameof(NoClipSettingsEventType.OnLocalLatencyNoClipSpeedChanged));
        }

        #endregion
    }
}
