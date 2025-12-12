using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonDependency(typeof(PermissionManagerAPI))]
    [CustomRaisedEventsDispatcher(typeof(PlayersBackendEventAttribute), typeof(PlayersBackendEventType))]
    public class PlayersBackendManager : PlayersBackendManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private int rpPlayerDataIndex;

        private void Start()
        {
            playerDataManager.RegisterCustomPlayerData<RPPlayerData>(nameof(RPPlayerData));
        }

        private void FetchPlayerDataClassIndex()
        {
            rpPlayerDataIndex = playerDataManager.GetPlayerDataClassNameIndex<RPPlayerData>(nameof(RPPlayerData));
        }

        [PlayerDataEvent(PlayerDataEventType.OnPrePlayerDataManagerInit)]
        public void OnPrePlayerDataManagerInit()
        {
            FetchPlayerDataClassIndex();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            FetchPlayerDataClassIndex();
        }

        public override void SendSetOverriddenDisplayNameIA(RPPlayerData rpPlayerData, string overriddenDisplayName)
        {
            if (overriddenDisplayName != null)
                overriddenDisplayName = overriddenDisplayName.Trim();
            lockstep.WriteSmallUInt(rpPlayerData.core.persistentId);
            lockstep.WriteString(overriddenDisplayName);
            lockstep.SendInputAction(setOverriddenDisplayNameIAId);
        }

        [HideInInspector][SerializeField] private uint setOverriddenDisplayNameIAId;
        [LockstepInputAction(nameof(setOverriddenDisplayNameIAId))]
        public void OnSetOverriddenDisplayNameIA()
        {
            uint persistentId = lockstep.ReadSmallUInt();
            if (!playerDataManager.TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData core))
                return;
            string overriddenDisplayName = lockstep.ReadString();
            SetOverriddenDisplayNameInGS((RPPlayerData)core.customPlayerData[rpPlayerDataIndex], overriddenDisplayName);
        }

        public override void SetOverriddenDisplayNameInGS(RPPlayerData rpPlayerData, string overriddenDisplayName)
        {
            if (rpPlayerData.core.isDeleted) // Never the case when coming from OnSetOverriddenDisplayNameIA.
                return;
            if (overriddenDisplayName != null)
                overriddenDisplayName = overriddenDisplayName.Trim();
            string prev = rpPlayerData.overriddenDisplayName;
            if (prev == overriddenDisplayName)
                return;
            rpPlayerData.overriddenDisplayName = overriddenDisplayName;
            RaiseOnRPPlayerDataOverriddenDisplayNameChanged(rpPlayerData, prev);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPPlayerDataOverriddenDisplayNameChangedListeners;

        private RPPlayerData rpPlayerDataForEvent;
        public override RPPlayerData RPPlayerDataForEvent => rpPlayerDataForEvent;
        private string previousOverriddenDisplayName;
        public override string PreviousOverriddenDisplayName => previousOverriddenDisplayName;

        private void RaiseOnRPPlayerDataOverriddenDisplayNameChanged(RPPlayerData rpPlayerDataForEvent, string previousOverriddenDisplayName)
        {
            this.rpPlayerDataForEvent = rpPlayerDataForEvent;
            this.previousOverriddenDisplayName = previousOverriddenDisplayName;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPPlayerDataOverriddenDisplayNameChangedListeners, nameof(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged));
            this.rpPlayerDataForEvent = null; // To prevent misuse of the API.
            this.previousOverriddenDisplayName = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
