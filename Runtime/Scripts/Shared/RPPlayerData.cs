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
        /// <para><see cref="RPPlayerData"/> player => <see langword="true"/></para>
        /// </summary>
        [System.NonSerialized] public DataDictionary favoritePlayersOutgoingLut = new DataDictionary();
        [System.NonSerialized] public RPPlayerData[] favoritePlayersOutgoing = new RPPlayerData[ArrList.MinCapacity];
        [System.NonSerialized] public int favoritePlayersOutgoingCount = 0;
        [System.NonSerialized] public RPPlayerData[] favoritePlayersIncoming = new RPPlayerData[ArrList.MinCapacity];
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
            if (isAboutToBeImported)
                return;
            characterName = "";
        }

        public override void OnPlayerDataUninit(bool force)
        {
            ((Internal.PlayersFavoritesManager)playersFavoritesManager).OnPlayerDataUnInit(this);
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return overriddenDisplayName != null || characterName != "";
        }

        private void WriteFavoriteItems()
        {
            lockstep.WriteSmallUInt((uint)favoriteItemsCount);
            for (int i = 0; i < favoriteItemsCount; i++)
                lockstep.WriteSmallUInt(favoriteItems[i].Id);
        }

        private void ReadFavoriteItems(bool isImport)
        {
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

        public override void Serialize(bool isExport)
        {
            lockstep.WriteString(overriddenDisplayName);
            lockstep.WriteString(characterName);

            WriteFavoriteItems();

            if (isExport)
                return;

            lockstep.WriteSmallUInt((uint)favoritePlayersOutgoingCount);
            for (int i = 0; i < favoritePlayersOutgoingCount; i++)
                playersBackendManager.WriteRPPlayerDataRef(favoritePlayersOutgoing[i]);

            // Must also serialize this list, it is not redundant, since the order must be identical on all clients.
            lockstep.WriteSmallUInt((uint)favoritePlayersIncomingCount);
            for (int i = 0; i < favoritePlayersIncomingCount; i++)
                playersBackendManager.WriteRPPlayerDataRef(favoritePlayersIncoming[i]);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            overriddenDisplayName = lockstep.ReadString();
            characterName = lockstep.ReadString();

            ReadFavoriteItems(isImport);

            if (isImport)
                return;

            favoritePlayersOutgoingCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref favoritePlayersOutgoing, favoritePlayersOutgoingCount);
            for (int i = 0; i < favoritePlayersOutgoingCount; i++)
            {
                RPPlayerData player = playersBackendManager.ReadRPPlayerDataRef();
                favoritePlayersOutgoing[i] = player;
                favoritePlayersOutgoingLut.Add(player, true);
            }

            favoritePlayersIncomingCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref favoritePlayersIncoming, favoritePlayersIncomingCount);
            for (int i = 0; i < favoritePlayersIncomingCount; i++)
                favoritePlayersIncoming[i] = playersBackendManager.ReadRPPlayerDataRef();
        }
    }
}
