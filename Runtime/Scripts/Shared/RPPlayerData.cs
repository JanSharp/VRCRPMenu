using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RPPlayerData : PlayerData
    {
        public override string PlayerDataInternalName => "jansharp.rp-menu-backend";
        public override string PlayerDataDisplayName => "RP Player Backend";
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersFavoritesManagerAPI playersFavoritesManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemsFavoritesManagerAPI itemsFavoritesManager;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;

        #region GameState
        /// <summary>
        /// <para><see langword="null"/> means it is not overridden, using
        /// <see cref="CorePlayerData.displayName"/> instead.</para>
        /// </summary>
        [System.NonSerialized] public string overriddenDisplayName;
        /// <summary>
        /// <para>Never <see langword="null"/>.</para>
        /// </summary>
        [System.NonSerialized] public string characterName;

        /// <summary>
        /// <para><see cref="CorePlayerData"/> player => <see langword="true"/></para>
        /// </summary>
        [System.NonSerialized] public DataDictionary favoritePlayersOutgoingLut = new DataDictionary();
        [System.NonSerialized] public CorePlayerData[] favoritePlayersOutgoing = new CorePlayerData[ArrList.MinCapacity];
        [System.NonSerialized] public int favoritePlayersOutgoingCount = 0;
        [System.NonSerialized] public CorePlayerData[] favoritePlayersIncoming = new CorePlayerData[ArrList.MinCapacity];
        [System.NonSerialized] public int favoritePlayersIncomingCount = 0;

        /// <summary>
        /// <para><see cref="uint"/> entityPrototypeId => <see langword="true"/></para>
        /// </summary>
        [System.NonSerialized] public DataDictionary favoriteItemIdsLut = new DataDictionary();
        [System.NonSerialized] public EntityPrototype[] favoriteItems = new EntityPrototype[ArrList.MinCapacity];
        [System.NonSerialized] public int favoriteItemsCount = 0;
        #endregion

        [System.NonSerialized] public uint[] importedFavoriteItemIds;

        public string PlayerDisplayName => overriddenDisplayName ?? core.displayName;
        public string PlayerDisplayNameWithCharacterName => characterName == ""
            ? $"[{PlayerDisplayName}]"
            : $"[{PlayerDisplayName}]  {characterName}"; // Intentional double space.

        public override void OnPlayerDataInit(bool isAboutToBeImported)
        {
            if (!isAboutToBeImported
                || !playersBackendManager.OptionsFromExport.includeCharacterName
                || !playersBackendManager.ImportOptions.includeCharacterName)
            {
                characterName = "";
            }
        }

        public override void OnPlayerDataUninit(bool force)
        {
            ((Internal.PlayersFavoritesManager)playersFavoritesManager).OnPlayerDataUnInit(this);
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return overriddenDisplayName != null
                || characterName != ""
                || favoritePlayersOutgoingCount != 0
                || favoritePlayersIncomingCount != 0
                || favoriteItemsCount != 0;
        }

        public override bool PersistPlayerDataInExport()
        {
            PlayersBackendImportExportOptions exportOptions = playersBackendManager.ExportOptions;
            // Flipping these conditions would be more performant, however this is more readable and this runs
            // in a block in the player data system checking if it should spread work out across frames so it's fine.
            return exportOptions.includeOverriddenDisplayName && overriddenDisplayName != null
                || exportOptions.includeCharacterName && characterName != ""
                || exportOptions.includeCharacterName && (favoritePlayersOutgoingCount != 0 || favoritePlayersIncomingCount != 0)
                || exportOptions.includeFavoriteItems && favoriteItemsCount != 0;
        }

        private void WriteString(bool isExport, bool includedInExport, string value)
        {
            if (!isExport || includedInExport)
                lockstep.WriteString(value);
        }

        private void ReadString(bool isImport, bool includedInExport, bool includedInImport, ref string value)
        {
            if (!isImport)
            {
                value = lockstep.ReadString();
                return;
            }
            if (!includedInExport)
                return;
            if (includedInImport)
                value = lockstep.ReadString();
            else
                lockstep.ReadString();
        }

        private void WriteFavoriteItems()
        {
            lockstep.WriteSmallUInt((uint)favoriteItemsCount);
            for (int i = 0; i < favoriteItemsCount; i++)
                lockstep.WriteSmallUInt(favoriteItems[i].Id);
        }

        private void ReadFavoriteItems(bool isImport, bool discard)
        {
            if (discard)
            {
                int count = (int)lockstep.ReadSmallUInt();
                for (int i = 0; i < count; i++)
                    lockstep.ReadSmallUInt();
                return;
            }

            if (isImport)
            {
                int count = (int)lockstep.ReadSmallUInt();
                importedFavoriteItemIds = new uint[count];
                for (int i = 0; i < count; i++)
                    importedFavoriteItemIds[i] = lockstep.ReadSmallUInt();
                ((Internal.ItemsFavoritesManager)itemsFavoritesManager).OnPlayerDataImported(this);
            }
            else
            {
                favoriteItemsCount = (int)lockstep.ReadSmallUInt();
                ArrList.EnsureCapacity(ref favoriteItems, favoriteItemsCount);
                for (int i = 0; i < favoriteItemsCount; i++)
                {
                    uint id = lockstep.ReadSmallUInt();
                    favoriteItems[i] = entitySystem.GetEntityPrototype(id);
                    favoriteItemIdsLut.Add(id, true);
                }
            }
        }

        private void WriteFavoritePlayers()
        {
            lockstep.WriteSmallUInt((uint)favoritePlayersOutgoingCount);
            for (int i = 0; i < favoritePlayersOutgoingCount; i++)
                playerDataManager.WriteCorePlayerDataRef(favoritePlayersOutgoing[i]);

            // Must also serialize this list, it is not redundant, since the order must be identical on all clients.
            lockstep.WriteSmallUInt((uint)favoritePlayersIncomingCount);
            for (int i = 0; i < favoritePlayersIncomingCount; i++)
                playerDataManager.WriteCorePlayerDataRef(favoritePlayersIncoming[i]);
        }

        private void ReadFavoritePlayers(bool isImport, bool discard)
        {
            if (discard)
            {
                for (int i = 0; i < 2; i++)
                {
                    int count = (int)lockstep.ReadSmallUInt();
                    for (int j = 0; j < count; j++)
                        playerDataManager.ReadCorePlayerDataRef(isImport);
                }
                return;
            }

            // Can use ReadCorePlayerDataRef reliably here, however could not reliably resolve refs to custom
            // player data. Those would have to be resolved in OnImportFinishingUp. But it is not necessary,
            // core player data is more than sufficient.

            favoritePlayersOutgoingLut.Clear();
            favoritePlayersOutgoingCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref favoritePlayersOutgoing, favoritePlayersOutgoingCount);
            for (int i = 0; i < favoritePlayersOutgoingCount; i++)
            {
                CorePlayerData player = playerDataManager.ReadCorePlayerDataRef(isImport);
                favoritePlayersOutgoing[i] = player;
                favoritePlayersOutgoingLut.Add(player, true);
            }

            favoritePlayersIncomingCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref favoritePlayersIncoming, favoritePlayersIncomingCount);
            for (int i = 0; i < favoritePlayersIncomingCount; i++)
                favoritePlayersIncoming[i] = playerDataManager.ReadCorePlayerDataRef(isImport);
        }

        public override void Serialize(bool isExport)
        {
            PlayersBackendImportExportOptions exportOptions = playersBackendManager.ExportOptions;

            WriteString(isExport, isExport && exportOptions.includeOverriddenDisplayName, overriddenDisplayName);
            WriteString(isExport, isExport && exportOptions.includeCharacterName, characterName);

            if (!isExport || exportOptions.includeFavoriteItems)
                WriteFavoriteItems();

            if (!isExport || exportOptions.includeFavoritePlayers)
                WriteFavoritePlayers();
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            PlayersBackendImportExportOptions optionsFromExport = playersBackendManager.OptionsFromExport;
            PlayersBackendImportExportOptions importOptions = playersBackendManager.ImportOptions;

            ReadString(
                isImport,
                isImport && optionsFromExport.includeOverriddenDisplayName,
                isImport && importOptions.includeOverriddenDisplayName,
                ref overriddenDisplayName);
            ReadString(
                isImport,
                isImport && optionsFromExport.includeCharacterName,
                isImport && importOptions.includeCharacterName,
                ref characterName);

            if (!isImport)
                ReadFavoriteItems(isImport, discard: false);
            else if (optionsFromExport.includeFavoriteItems)
                ReadFavoriteItems(isImport, discard: !importOptions.includeFavoriteItems);

            if (!isImport)
                ReadFavoritePlayers(isImport, discard: false);
            else if (optionsFromExport.includeFavoritePlayers)
                ReadFavoritePlayers(isImport, discard: !importOptions.includeFavoritePlayers);
        }
    }
}
