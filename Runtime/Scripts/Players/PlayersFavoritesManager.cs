using UdonSharp;
using UnityEngine;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(PlayersFavoritesEventAttribute), typeof(PlayersFavoritesEventType))]
    public class PlayersFavoritesManager : PlayersFavoritesManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;

        public override void SendAddFavoritePlayerIA(RPPlayerData source, RPPlayerData target)
        {
            if (!lockstep.IsInitialized)
                return;
            playersBackendManager.WriteRPPlayerDataRef(source);
            playersBackendManager.WriteRPPlayerDataRef(target);
            lockstep.SendInputAction(addFavoritePlayerIAId);
        }

        [HideInInspector][SerializeField] private uint addFavoritePlayerIAId;
        [LockstepInputAction(nameof(addFavoritePlayerIAId))]
        public void OnAddFavoritePlayerIA()
        {
            RPPlayerData source = playersBackendManager.ReadRPPlayerDataRef();
            RPPlayerData target = playersBackendManager.ReadRPPlayerDataRef();
            if (source == null || target == null)
                return;
            if (source.favoritePlayersOutgoingLut.ContainsKey(target))
                return;
            source.favoritePlayersOutgoingLut.Add(target, true);
            ArrList.Add(ref source.favoritePlayersOutgoing, ref source.favoritePlayersOutgoingCount, target);
            ArrList.Add(ref target.favoritePlayersIncoming, ref target.favoritePlayersIncomingCount, source);
            RaiseOnPlayerFavoriteAdded(source, target);
        }

        public override void SendRemoveFavoritePlayerIA(RPPlayerData source, RPPlayerData target)
        {
            if (!lockstep.IsInitialized)
                return;
            playersBackendManager.WriteRPPlayerDataRef(source);
            playersBackendManager.WriteRPPlayerDataRef(target);
            lockstep.SendInputAction(removeFavoritePlayerIAId);
        }

        [HideInInspector][SerializeField] private uint removeFavoritePlayerIAId;
        [LockstepInputAction(nameof(removeFavoritePlayerIAId))]
        public void OnRemoveFavoritePlayerIA()
        {
            RPPlayerData source = playersBackendManager.ReadRPPlayerDataRef();
            RPPlayerData target = playersBackendManager.ReadRPPlayerDataRef();
            if (source == null || target == null)
                return;
            if (!source.favoritePlayersOutgoingLut.Remove(target))
                return;
            ArrList.Remove(ref source.favoritePlayersOutgoing, ref source.favoritePlayersOutgoingCount, target);
            ArrList.Remove(ref target.favoritePlayersIncoming, ref target.favoritePlayersIncomingCount, source);
            RaiseOnPlayerFavoriteRemoved(source, target);
        }

        /// <summary>
        /// <para>Internal API.</para>
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerDataUnInit(RPPlayerData player)
        {
            // At the time of writing this the Player Data system does not delete any existing player data
            // during imports, however if it did then this here should actually handle it cleanly anyway,
            // nothing needs to happen in OnImportFinishingUp. The only unfortunate part would be that this
            // would raise events during the import process, which I have kind of started to avoid, but it
            // would be fine anyway as systems should expect to receive events involving unknown players.
            RPPlayerData[] players = player.favoritePlayersIncoming;
            int count = player.favoritePlayersIncomingCount;
            for (int i = count - 1; i >= 0; i--)
            {
                RPPlayerData source = players[i];
                source.favoritePlayersOutgoingLut.Remove(player);
                ArrList.Remove(ref source.favoritePlayersOutgoing, ref source.favoritePlayersOutgoingCount, player);
                player.favoritePlayersIncomingCount = i;
                RaiseOnPlayerFavoriteRemoved(source, player);
            }
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerFavoriteAddedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerFavoriteRemovedListeners;

        public RPPlayerData sourcePlayerForEvent;
        public override RPPlayerData SourcePlayerForEvent => sourcePlayerForEvent;
        public RPPlayerData targetPlayerForEvent;
        public override RPPlayerData TargetPlayerForEvent => targetPlayerForEvent;

        private void RaiseOnPlayerFavoriteAdded(RPPlayerData sourcePlayerForEvent, RPPlayerData targetPlayerForEvent)
        {
            this.sourcePlayerForEvent = sourcePlayerForEvent;
            this.targetPlayerForEvent = targetPlayerForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerFavoriteAddedListeners, nameof(PlayersFavoritesEventType.OnPlayerFavoriteAdded));
            this.sourcePlayerForEvent = null; // To prevent misuse of the API.
            this.targetPlayerForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerFavoriteRemoved(RPPlayerData sourcePlayerForEvent, RPPlayerData targetPlayerForEvent)
        {
            this.sourcePlayerForEvent = sourcePlayerForEvent;
            this.targetPlayerForEvent = targetPlayerForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerFavoriteRemovedListeners, nameof(PlayersFavoritesEventType.OnPlayerFavoriteRemoved));
            this.sourcePlayerForEvent = null; // To prevent misuse of the API.
            this.targetPlayerForEvent = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
