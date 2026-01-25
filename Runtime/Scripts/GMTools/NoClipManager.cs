using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(NoClipEventAttribute), typeof(NoClipEventType))]
    public class NoClipManager : NoClipManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

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
            playerDataManager.RegisterCustomPlayerData<NoClipPlayerData>(nameof(NoClipPlayerData));
        }

        [PlayerDataEvent(PlayerDataEventType.OnAllCustomPlayerDataRegistered)]
        public void OnAllCustomPlayerDataRegistered()
        {
            noClipPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<NoClipPlayerData>(nameof(NoClipPlayerData));
        }

        /// <summary>
        /// <para>Internal api.</para>
        /// <para>Effectively gets called inside of <see cref="LockstepEventType.OnInit"/> and game state
        /// deserialization, in both cases <paramref name="suppressEvents"/> is <see langword="true"/>,
        /// therefore also making this the latency state initialization function.</para>
        /// </summary>
        /// <param name="localData"></param>
        /// <param name="suppressEvents"></param>
        public void ResetLatencyStateToGameState(NoClipPlayerData localData, bool suppressEvents)
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

        private bool ShouldApplyReceivedIAToLatencyState(NoClipPlayerData settings)
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

        public override void SendSetNoClipEnabledIA(NoClipPlayerData data, bool noClipEnabled)
        {
            WriteNoClipPlayerDataRef(data);
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
            NoClipPlayerData data = ReadNoClipPlayerDataRef();
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

        public override void SendSetNoClipSpeedIA(NoClipPlayerData data, float noClipSpeed)
        {
            WriteNoClipPlayerDataRef(data);
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
            NoClipPlayerData data = ReadNoClipPlayerDataRef();
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

        public override NoClipPlayerData LocalNoClipPlayerData => (NoClipPlayerData)playerDataManager.LocalPlayerData.customPlayerData[noClipPlayerDataIndex];

        public override NoClipPlayerData SendingNoClipPlayerData => (NoClipPlayerData)playerDataManager.SendingPlayerData.customPlayerData[noClipPlayerDataIndex];

        public override NoClipPlayerData GetNoClipPlayerData(CorePlayerData core) => (NoClipPlayerData)core.customPlayerData[noClipPlayerDataIndex];

        public override void WriteNoClipPlayerDataRef(NoClipPlayerData data)
        {
            playerDataManager.WriteCorePlayerDataRef(data == null ? null : data.core);
        }

        public override NoClipPlayerData ReadNoClipPlayerDataRef()
        {
            CorePlayerData core = playerDataManager.ReadCorePlayerDataRef();
            return core == null ? null : (NoClipPlayerData)core.customPlayerData[noClipPlayerDataIndex];
        }

        #endregion

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyNoClipEnabledChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onLocalLatencyNoClipSpeedChangedListeners;

        private void RaiseOnLocalLatencyNoClipEnabledChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyNoClipEnabledChangedListeners, nameof(NoClipEventType.OnLocalLatencyNoClipEnabledChanged));
        }

        private void RaiseOnLocalLatencyNoClipSpeedChanged()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onLocalLatencyNoClipSpeedChangedListeners, nameof(NoClipEventType.OnLocalLatencyNoClipSpeedChanged));
        }

        #endregion
    }
}
