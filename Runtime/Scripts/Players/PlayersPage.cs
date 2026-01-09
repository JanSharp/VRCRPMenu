using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersPage : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersFavoritesManagerAPI playersFavoritesManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;

        public PlayersList rowsList;
        public PlayersRow rowPrefabScript;

        [PermissionDefinitionReference(nameof(viewCharacterNamePDef))]
        public string viewCharacterNamePermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewCharacterNamePDef;

        [PermissionDefinitionReference(nameof(teleportToPlayerPDef))]
        public string teleportToPlayerPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition teleportToPlayerPDef;

        [PermissionDefinitionReference(nameof(viewPlayerProximityPDef))]
        public string viewPlayerProximityPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewPlayerProximityPDef;

        [PermissionDefinitionReference(nameof(playerSelectionPDef))]
        public string playerSelectionPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition playerSelectionPDef;

        private uint localPlayerId;
        private bool isInitialized = false;

        private RPPlayerData localPlayer;

        /// <summary>
        /// <para>To avoid having to do this using OnInit would be required, which makes putting a rebuild
        /// rows call into OnInit and checking isInitialized in every event handler.</para>
        /// </summary>
        private void FetchLocalPlayer()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.GetCorePlayerDataForPlayerId(localPlayerId));
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
        }

        [PlayerDataEvent(PlayerDataEventType.OnPrePlayerDataManagerInit)]
        public void OnPrePlayerDataManagerInit()
        {
            rowsList.Initialize();
            isInitialized = true;
        }

        // [LockstepEvent(LockstepEventType.OnInit)]
        // public void OnInit()
        // {
        // }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            isInitialized = true;
        }

        // [LockstepEvent(LockstepEventType.OnImportStart)]
        // public void OnImportStart()
        // {
        // }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            if (playerDataManager.IsPartOfCurrentImport)
            {
                RebuildRows(); // Runs ShowOnlyRowsVisibleInViewport, do not need to call it manually here.
                if (lockstep.FlaggedToContinueNextFrame)
                    return;
            }
        }

        #region PermissionResolution

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            bool characterNameValue = viewCharacterNamePDef.valueForLocalPlayer;
            bool teleportToValue = teleportToPlayerPDef.valueForLocalPlayer;
            bool proximityValue = viewPlayerProximityPDef.valueForLocalPlayer;
            bool selectionValue = playerSelectionPDef.valueForLocalPlayer;

            rowsList.SortOnPermissionChange(characterNameValue, proximityValue, selectionValue);

            // if (!permissionGroupValue)
            //     EnsureClosedPermissionGroupPopup();
            // if (!deleteValue)
            //     EnsureClosedConfirmDeletePopup();

            bool characterNameChanged = rowPrefabScript.characterNameRoot.activeSelf != characterNameValue;
            bool teleportToChanged = rowPrefabScript.teleportToRoot.activeSelf != teleportToValue;
            bool proximityChanged = rowPrefabScript.proximityRoot.activeSelf != proximityValue;
            bool selectionChanged = rowPrefabScript.selectRoot.activeSelf != selectionValue;
            rowPrefabScript.characterNameRoot.SetActive(characterNameValue);
            rowPrefabScript.teleportToRoot.SetActive(teleportToValue);
            rowPrefabScript.proximityRoot.SetActive(proximityValue);
            rowPrefabScript.selectRoot.SetActive(selectionValue);

            PlayersRow[] rows = rowsList.Rows;
            int rowsCount = rowsList.RowsCount;
            for (int i = 0; i < rowsCount; i++)
            {
                // I'm thinking that in any case where 2 permissions changed it is faster to do all 4 ifs
                // every loop, because I have heard that Udon arrays are slow. But it's just a guess.
                PlayersRow row = rows[i];
                if (characterNameChanged)
                    row.characterNameRoot.SetActive(characterNameValue);
                if (teleportToChanged)
                    row.teleportToRoot.SetActive(teleportToValue);
                if (proximityChanged)
                    row.proximityRoot.SetActive(proximityValue);
                if (selectionChanged)
                    row.selectRoot.SetActive(selectionValue);
            }
        }

        #endregion

        #region RowsManagement

        private bool TryGetRow(uint persistentId, out PlayersRow row) => rowsList.TryGetRow(persistentId, out row);

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataCreated)]
        public void OnPlayerDataCreated() => CreatePlayerRowForEvent();

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOnline)]
        public void OnPlayerDataWentOnline() => CreatePlayerRowForEvent();

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline() => DeletePlayerRowForEvent();

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted() => DeletePlayerRowForEvent();

        private void CreatePlayerRowForEvent()
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            CorePlayerData core = playerDataManager.PlayerDataForEvent;
#if !RP_MENU_SHOW_OFFLINE_PLAYERS_IN_PLAYER_LIST
            if (core.isOffline)
                return;
#endif
            rowsList.CreateRow(core);
        }

        private void DeletePlayerRowForEvent()
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            CorePlayerData core = playerDataManager.PlayerDataForEvent;
            if (!TryGetRow(core.persistentId, out PlayersRow row))
                return;
            rowsList.RemoveRow(row);
        }

        private void RebuildRows()
        {
            // if (!lockstep.IsContinuationFromPrevFrame)
            //     EnsureClosedPopups();
            rowsList.RebuildRows();
        }

        #endregion

        #region Favorite

        public void OnFavoriteValueChanged(PlayersRow row)
        {
            if (localPlayer == null)
                FetchLocalPlayer();
            bool isFavorite = row.favoriteToggle.isOn;
            if (isFavorite)
                playersFavoritesManager.SendAddFavoritePlayerIA(localPlayer, row.rpPlayerData);
            else
                playersFavoritesManager.SendRemoveFavoritePlayerIA(localPlayer, row.rpPlayerData);
            // Latency hiding.
            row.isFavorite = isFavorite;
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        [PlayersFavoritesEvent(PlayersFavoritesEventType.OnPlayerFavoriteAdded)]
        public void OnPlayerFavoriteAdded() => OnPlayerFavoriteChanged(true);

        [PlayersFavoritesEvent(PlayersFavoritesEventType.OnPlayerFavoriteRemoved)]
        public void OnPlayerFavoriteRemoved() => OnPlayerFavoriteChanged(false);

        private void OnPlayerFavoriteChanged(bool isFavorite)
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            if (!playersFavoritesManager.SourcePlayerForEvent.core.isLocal)
                return;
            if (!TryGetRow(playersFavoritesManager.TargetPlayerForEvent.core.persistentId, out PlayersRow row))
                return;
            row.isFavorite = isFavorite;
            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        #endregion

        #region PlayerName

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged)]
        public void OnRPPlayerDataOverriddenDisplayNameChanged()
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!TryGetRow(rpPlayerData.core.persistentId, out PlayersRow row))
                return; // Some system did something weird.
            string playerName = rpPlayerData.PlayerDisplayName;
            row.sortablePlayerName = playerName.ToLower();
            row.playerNameLabel.text = playerName;
            rowsList.PotentiallySortChangedPlayerNameRow(row);
        }

        #endregion

        #region CharacterName

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged)]
        public void OnRPPlayerDataCharacterNameChanged()
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            if (!TryGetRow(rpPlayerData.core.persistentId, out PlayersRow row))
                return; // Some system did something weird.
            string characterName = rpPlayerData.characterName;
            row.sortableCharacterName = characterName.ToLower();
            row.characterNameLabel.text = characterName;
            rowsList.PotentiallySortChangedCharacterNameRow(row);
        }

        #endregion

        #region TeleportTo

        public void OnTeleportToClick(PlayersRow row)
        {
            // TODO: impl
        }

        #endregion

        #region Selection

        public void OnSelectValueChanged(PlayersRow row)
        {
            selectionManager.SetPlayerSelectionState(row.rpPlayerData.core, row.selectToggle.isOn);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnOnePlayerSelectionChanged)]
        public void OnOnePlayerSelectionChanged()
        {
            CorePlayerData player = selectionManager.ChangedPlayerForEvent;
            if (!TryGetRow(selectionManager.ChangedPlayerForEvent.persistentId, out PlayersRow row))
                return; // Some system did something weird.
            row.selectToggle.SetIsOnWithoutNotify(selectionManager.selectedPlayersLut.ContainsKey(player));
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnMultiplePlayerSelectionChanged)]
        public void OnMultiplePlayerSelectionChanged()
        {
            DataDictionary selectedPlayersLut = selectionManager.selectedPlayersLut;
            PlayersRow[] rows = rowsList.Rows;
            int count = rowsList.RowsCount;
            for (int i = 0; i < count; i++)
            {
                PlayersRow row = rows[i];
                row.selectToggle.SetIsOnWithoutNotify(selectedPlayersLut.ContainsKey(row.rpPlayerData.core));
            }
        }

        #endregion
    }
}
