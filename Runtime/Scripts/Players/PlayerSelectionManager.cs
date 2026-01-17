using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    public enum PlayerSelectionEventType
    {
        OnOnePlayerSelectionChanged,
        OnMultiplePlayerSelectionChanged,
        OnSelectionGroupPlayerRemoved,
        OnSelectionGroupAdded,
        OnSelectionGroupOverwritten,
        OnSelectionGroupDeleted,
        OnSelectionGroupUndoOverwriteStackChanged,
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

        /// <summary>
        /// <para>When the RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST, which exists for debugging, this will
        /// contain offline players too.</para>
        /// </summary>
        [System.NonSerialized] public CorePlayerData[] allOnlinePlayers = new CorePlayerData[ArrList.MinCapacity];
        [System.NonSerialized] public int allOnlinePlayersCount = 0;

        [System.NonSerialized] public DataDictionary selectedPlayersLut = new DataDictionary();
        [System.NonSerialized] public CorePlayerData[] selectedPlayers = new CorePlayerData[ArrList.MinCapacity];
        [System.NonSerialized] public int selectedPlayersCount = 0;

        public PlayerSelectionGroup SelectionGroupForSerialization => (PlayerSelectionGroup)dataForSerialization;

        private uint localPlayerId;
        private PerPlayerSelectionData localPlayer;
        public PerPlayerSelectionData LocalPlayer
        {
            get
            {
                if (localPlayer == null)
                    localPlayer = (PerPlayerSelectionData)playerDataManager.GetCorePlayerDataForPlayerId(localPlayerId).customPlayerData[playerDataIndex];
                return localPlayer;
            }
        }

        protected override void Start()
        {
            base.Start();
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
        }

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

        public void SetSelectedPlayers(CorePlayerData[] players, int count)
        {
            selectedPlayersLut = new DataDictionary();
            ArrList.Clear(ref selectedPlayers, ref selectedPlayersCount);
            for (int i = 0; i < count; i++)
            {
                CorePlayerData player = players[i];
#if !RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
                if (player.isOffline)
                    continue;
#endif
                selectedPlayersLut.Add(player, true);
                ArrList.Add(ref selectedPlayers, ref selectedPlayersCount, player);
            }
            RaiseOnMultiplePlayerSelectionChanged();
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

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp, Order = -10)] // Run before row rebuilding.
        public void OnClientBeginCatchUp()
        {
#if RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
            PopulateOnlinePlayersWithAllPlayers();
#else
            CorePlayerData[] players = playerDataManager.AllCorePlayerDataRaw;
            int playersCount = playerDataManager.AllCorePlayerDataCount;
            for (int i = 0; i < playersCount; i++)
            {
                CorePlayerData player = players[i];
                if (player.isOffline)
                    continue;
                ArrList.Add(ref allOnlinePlayers, ref allOnlinePlayersCount, player);
            }
#endif
        }

#if RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
        private void PopulateOnlinePlayersWithAllPlayers()
        {
            CorePlayerData[] players = playerDataManager.AllCorePlayerDataRaw;
            allOnlinePlayersCount = playerDataManager.AllCorePlayerDataCount;
            ArrList.EnsureCapacity(ref allOnlinePlayers, allOnlinePlayersCount);
            System.Array.Copy(players, allOnlinePlayers, allOnlinePlayersCount);
        }
#endif

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated()
        {
            CorePlayerData player = playerDataManager.PlayerDataForEvent;
#if !RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
            if (player.isOffline)
                return;
#endif
            ArrList.Add(ref allOnlinePlayers, ref allOnlinePlayersCount, player);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted()
        {
            RemovePlayerForEvent();
        }

#if !RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline()
        {
            ArrList.Add(ref allOnlinePlayers, ref allOnlinePlayersCount, playerDataManager.PlayerDataForEvent);
        }
#endif

#if !RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline()
        {
            RemovePlayerForEvent();
        }
#endif

        private void RemovePlayerForEvent()
        {
            CorePlayerData deletedPlayer = playerDataManager.PlayerDataForEvent;
            ArrList.Remove(ref allOnlinePlayers, ref allOnlinePlayersCount, deletedPlayer);
            if (selectedPlayersLut.Remove(deletedPlayer))
            {
                ArrList.Remove(ref selectedPlayers, ref selectedPlayersCount, deletedPlayer);
                RaiseOnOnePlayerSelectionChanged(deletedPlayer);
            }

            CorePlayerData[] players = playerDataManager.AllCorePlayerDataRaw;
            int playersCount = playerDataManager.AllCorePlayerDataCount;
            for (int i = 0; i < playersCount; i++)
            {
                PerPlayerSelectionData player = (PerPlayerSelectionData)players[i].customPlayerData[playerDataIndex];
                RemoveDeletedPlayerFromGroups((PlayerSelectionGroup[])player.localDynamicData, player.localDynamicDataCount, deletedPlayer);
            }
            RemoveDeletedPlayerFromGroups((PlayerSelectionGroup[])globalDynamicData, globalDynamicDataCount, deletedPlayer);
        }

        private void RemoveDeletedPlayerFromGroups(PlayerSelectionGroup[] groups, int groupsCount, CorePlayerData deletedPlayer)
        {
            for (int i = 0; i < groupsCount; i++)
            {
                PlayerSelectionGroup group = groups[i];
                CorePlayerData[] selectedPlayers = group.selectedPlayers;
                int length = selectedPlayers.Length;
                int index = System.Array.IndexOf(selectedPlayers, deletedPlayer);
                if (index == -1)
                    continue;
                CorePlayerData[] newSelectedPlayers = new CorePlayerData[length - 1];
                System.Array.Copy(selectedPlayers, 0, newSelectedPlayers, 0, index);
                System.Array.Copy(selectedPlayers, index + 1, newSelectedPlayers, index, length - index - 1);
                group.selectedPlayers = newSelectedPlayers;
                RaiseOnSelectionGroupPlayerRemoved(group, deletedPlayer);
            }
        }

#if RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
        [LockstepEvent(LockstepEventType.OnImportFinishingUp, Order = -10)] // Run before row rebuilding.
        public void OnImportFinishingUp()
        {
            PopulateOnlinePlayersWithAllPlayers();
            // At the time of writing this the player data system does not delete existing offline player data
            // during imports even if tha player data was not part of the import, thus there is no need to
            // check if any players must be removed from the current selection.
            // This also applies to when RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST is not set, which is
            // ultimately why the event handler is preprocessed out entirely in that case.
        }
#endif

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onOnePlayerSelectionChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onMultiplePlayerSelectionChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onSelectionGroupPlayerRemovedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onSelectionGroupAddedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onSelectionGroupOverwrittenListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onSelectionGroupDeletedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onSelectionGroupUndoOverwriteStackChangedListeners;

        private CorePlayerData playerDataForEvent;
        public CorePlayerData PlayerDataForEvent => playerDataForEvent;

        private PlayerSelectionGroup selectionGroupForEvent;
        public PlayerSelectionGroup SelectionGroupForEvent => selectionGroupForEvent;

        private PlayerSelectionGroup overwrittenSelectionGroupForEvent;
        public PlayerSelectionGroup OverwrittenSelectionGroupForEvent => overwrittenSelectionGroupForEvent;

        private void RaiseOnOnePlayerSelectionChanged(CorePlayerData changedPlayerForEvent)
        {
            this.playerDataForEvent = changedPlayerForEvent;
            CustomRaisedEvents.Raise(ref onOnePlayerSelectionChangedListeners, nameof(PlayerSelectionEventType.OnOnePlayerSelectionChanged));
            this.playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnMultiplePlayerSelectionChanged()
        {
            CustomRaisedEvents.Raise(ref onMultiplePlayerSelectionChangedListeners, nameof(PlayerSelectionEventType.OnMultiplePlayerSelectionChanged));
        }

        private void RaiseOnSelectionGroupPlayerRemoved(PlayerSelectionGroup selectionGroupForEvent, CorePlayerData playerDataForEvent)
        {
            this.selectionGroupForEvent = selectionGroupForEvent;
            this.playerDataForEvent = playerDataForEvent;
            CustomRaisedEvents.Raise(ref onSelectionGroupPlayerRemovedListeners, nameof(PlayerSelectionEventType.OnSelectionGroupPlayerRemoved));
            this.selectionGroupForEvent = null; // To prevent misuse of the API.
            this.playerDataForEvent = null; // To prevent misuse of the API.
        }

        protected override void RaiseOnDataAdded(DynamicData data)
        {
            selectionGroupForEvent = (PlayerSelectionGroup)data;
            CustomRaisedEvents.Raise(ref onSelectionGroupAddedListeners, nameof(PlayerSelectionEventType.OnSelectionGroupAdded));
            selectionGroupForEvent = null; // To prevent misuse of the API.
        }

        protected override void RaiseOnDataOverwritten(DynamicData data, DynamicData overwrittenData)
        {
            selectionGroupForEvent = (PlayerSelectionGroup)data;
            overwrittenSelectionGroupForEvent = (PlayerSelectionGroup)overwrittenData;
            CustomRaisedEvents.Raise(ref onSelectionGroupOverwrittenListeners, nameof(PlayerSelectionEventType.OnSelectionGroupOverwritten));
            selectionGroupForEvent = null; // To prevent misuse of the API.
            overwrittenSelectionGroupForEvent = null; // To prevent misuse of the API.
        }

        protected override void RaiseOnDataDeleted(DynamicData data)
        {
            selectionGroupForEvent = (PlayerSelectionGroup)data;
            CustomRaisedEvents.Raise(ref onSelectionGroupDeletedListeners, nameof(PlayerSelectionEventType.OnSelectionGroupDeleted));
            selectionGroupForEvent = null; // To prevent misuse of the API.
        }

        protected override void RaiseOnOverwriteUndoStackChanged()
        {
            CustomRaisedEvents.Raise(ref onSelectionGroupUndoOverwriteStackChangedListeners, nameof(PlayerSelectionEventType.OnSelectionGroupUndoOverwriteStackChanged));
        }

        #endregion
    }
}
