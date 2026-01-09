using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    public enum PlayerSelectionEventType
    {
        OnOnePlayerSelectionChanged,
        OnMultiplePlayerSelectionChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PlayerSelectionEventAttribute : CustomRaisedEventBaseAttribute
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
        public PlayerSelectionEventAttribute(PlayerSelectionEventType eventType)
            : base((int)eventType)
        { }
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonScript("95263ec555dec07b5914eaca0c50884a")] // Runtime/Prefabs/Managers/PlayerSelectionManager.prefab
    [CustomRaisedEventsDispatcher(typeof(PlayerSelectionEventAttribute), typeof(PlayerSelectionEventType))]
    public class PlayerSelectionManager : DynamicDataManager
    {
        public override string GameStateInternalName => "jansharp.rp-menu-player-selection";
        public override string GameStateDisplayName => "Player Selection";
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;

        public override string DynamicDataClassName => nameof(PlayerSelectionGroup);
        public override string PerPlayerDataClassName => nameof(PerPlayerSelectionData);

        private CorePlayerData[] allOnlinePlayers = new CorePlayerData[ArrList.MinCapacity];
        private int allOnlinePlayersCount = 0;

        [System.NonSerialized] public DataDictionary selectedPlayersLut = new DataDictionary();
        [System.NonSerialized] public CorePlayerData[] selectedPlayers = new CorePlayerData[ArrList.MinCapacity];
        [System.NonSerialized] public int selectedPlayersCount = 0;

        public void SetPlayerSelectionState(CorePlayerData player, bool isSelected)
        {
            bool wasSelected = selectedPlayersLut.ContainsKey(player);
            if (wasSelected == isSelected)
                return;
            if (isSelected)
            {
                selectedPlayersLut.Add(player, true);
                ArrList.Add(ref selectedPlayers, ref selectedPlayersCount, player);
            }
            else
            {
                selectedPlayersLut.Remove(player);
                ArrList.Remove(ref selectedPlayers, ref selectedPlayersCount, player);
            }
            RaiseOnOnePlayerSelectionChanged(player);
        }

        public void SelectNone()
        {
            if (selectedPlayersCount == 0)
                return;
            selectedPlayersLut.Clear();
            ArrList.Clear(ref selectedPlayers, ref selectedPlayersCount);
            RaiseOnMultiplePlayerSelectionChanged();
        }

        public void InvertSelection()
        {
            DataDictionary prevSelectedPlayersLut = selectedPlayersLut;
            selectedPlayersLut = new DataDictionary();
            ArrList.Clear(ref selectedPlayers, ref selectedPlayersCount);
            for (int i = 0; i < allOnlinePlayersCount; i++)
            {
                CorePlayerData player = allOnlinePlayers[i];
                if (prevSelectedPlayersLut.ContainsKey(player))
                    continue;
                selectedPlayersLut.Add(player, true);
                ArrList.Add(ref selectedPlayers, ref selectedPlayersCount, player);
            }
            RaiseOnMultiplePlayerSelectionChanged();
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            CorePlayerData player = playerDataManager.PlayerDataForEvent;
            if (player.isOffline)
                return;
            ArrList.Add(ref allOnlinePlayers, ref allOnlinePlayersCount, player);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            ArrList.Remove(ref allOnlinePlayers, ref allOnlinePlayersCount, playerDataManager.PlayerDataForEvent);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline()
        {
            ArrList.Add(ref allOnlinePlayers, ref allOnlinePlayersCount, playerDataManager.PlayerDataForEvent);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline()
        {
            ArrList.Remove(ref allOnlinePlayers, ref allOnlinePlayersCount, playerDataManager.PlayerDataForEvent);
        }

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onOnePlayerSelectionChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onMultiplePlayerSelectionChangedListeners;

        private CorePlayerData changedPlayerForEvent;
        public CorePlayerData ChangedPlayerForEvent => changedPlayerForEvent;

        private void RaiseOnOnePlayerSelectionChanged(CorePlayerData changedPlayerForEvent)
        {
            this.changedPlayerForEvent = changedPlayerForEvent;
            CustomRaisedEvents.Raise(ref onOnePlayerSelectionChangedListeners, nameof(PlayerSelectionEventType.OnOnePlayerSelectionChanged));
            this.changedPlayerForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnMultiplePlayerSelectionChanged()
        {
            CustomRaisedEvents.Raise(ref onMultiplePlayerSelectionChangedListeners, nameof(PlayerSelectionEventType.OnMultiplePlayerSelectionChanged));
        }

        #endregion
    }
}
