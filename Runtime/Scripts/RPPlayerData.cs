using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RPPlayerData : PlayerData
    {
        public override string PlayerDataInternalName => "jansharp.rp-backend";
        public override string PlayerDataDisplayName => "RP Player Backend";
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;

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
        #endregion

        public string PlayerDisplayName => overriddenDisplayName ?? core.displayName;

        public override bool PersistPlayerDataWhileOffline()
        {
            return overriddenDisplayName != null || characterName != "";
        }

        public override void OnPlayerDataInit(bool isAboutToBeImported)
        {
            if (isAboutToBeImported)
                return;
            characterName = "";
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteString(overriddenDisplayName);
            lockstep.WriteString(characterName);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            overriddenDisplayName = lockstep.ReadString();
            characterName = lockstep.ReadString();
        }
    }
}
