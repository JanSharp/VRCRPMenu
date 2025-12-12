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

        public override void SendSetCharacterNameIA(RPPlayerData rpPlayerData, string characterName)
        {
            if (characterName != null) // Just reducing network data, not like it makes literally any difference.
                characterName = characterName.Trim();
            lockstep.WriteSmallUInt(rpPlayerData.core.persistentId);
            lockstep.WriteString(characterName);
            lockstep.SendInputAction(setCharacterNameIAId);
        }

        [HideInInspector][SerializeField] private uint setCharacterNameIAId;
        [LockstepInputAction(nameof(setCharacterNameIAId))]
        public void OnSetCharacterNameIA()
        {
            uint persistentId = lockstep.ReadSmallUInt();
            if (!playerDataManager.TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData core))
                return;
            string characterName = lockstep.ReadString();
            SetCharacterNameInGS((RPPlayerData)core.customPlayerData[rpPlayerDataIndex], characterName);
        }

        public override void SetCharacterNameInGS(RPPlayerData rpPlayerData, string characterName)
        {
            if (rpPlayerData.core.isDeleted) // Never the case when coming from OnSetCharacterNameIA.
                return;
            characterName = characterName == null ? "" : characterName.Trim();
            string prev = rpPlayerData.characterName;
            if (prev == characterName)
                return;
            rpPlayerData.characterName = characterName;
            RaiseOnRPPlayerDataCharacterNameChanged(rpPlayerData, prev);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPPlayerDataOverriddenDisplayNameChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRPPlayerDataCharacterNameChangedListeners;

        private RPPlayerData rpPlayerDataForEvent;
        public override RPPlayerData RPPlayerDataForEvent => rpPlayerDataForEvent;
        private string previousOverriddenDisplayName;
        public override string PreviousOverriddenDisplayName => previousOverriddenDisplayName;
        private string previousCharacterName;
        public override string PreviousCharacterName => previousCharacterName;

        private void RaiseOnRPPlayerDataOverriddenDisplayNameChanged(RPPlayerData rpPlayerDataForEvent, string previousOverriddenDisplayName)
        {
            this.rpPlayerDataForEvent = rpPlayerDataForEvent;
            this.previousOverriddenDisplayName = previousOverriddenDisplayName;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPPlayerDataOverriddenDisplayNameChangedListeners, nameof(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged));
            this.rpPlayerDataForEvent = null; // To prevent misuse of the API.
            this.previousOverriddenDisplayName = null; // To prevent misuse of the API.
        }

        private void RaiseOnRPPlayerDataCharacterNameChanged(RPPlayerData rpPlayerDataForEvent, string previousCharacterName)
        {
            this.rpPlayerDataForEvent = rpPlayerDataForEvent;
            this.previousCharacterName = previousCharacterName;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRPPlayerDataCharacterNameChangedListeners, nameof(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged));
            this.rpPlayerDataForEvent = null; // To prevent misuse of the API.
            this.previousCharacterName = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
