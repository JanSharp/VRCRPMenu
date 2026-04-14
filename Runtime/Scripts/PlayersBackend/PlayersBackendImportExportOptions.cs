using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendImportExportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public bool includeOverriddenDisplayName = true;
        [System.NonSerialized] public bool includeCharacterName = true;
        [System.NonSerialized] public bool includeFavoriteItems = true;
        [System.NonSerialized] public bool includeFavoritePlayers = true;

        public override bool WannaBeClassSupportsPooling => true;
        public override void ResetWannaBeClassToDefault()
        {
            base.ResetWannaBeClassToDefault();
            includeOverriddenDisplayName = true;
            includeCharacterName = true;
            includeFavoriteItems = true;
            includeFavoritePlayers = true;
        }

        public override LockstepGameStateOptionsData Clone()
        {
            var clone = wannaBeClasses.New<PlayersBackendImportExportOptions>(nameof(PlayersBackendImportExportOptions));
            clone.includeOverriddenDisplayName = includeOverriddenDisplayName;
            clone.includeCharacterName = includeCharacterName;
            clone.includeFavoriteItems = includeFavoriteItems;
            clone.includeFavoritePlayers = includeFavoritePlayers;
            return clone;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(
                includeOverriddenDisplayName,
                includeCharacterName,
                includeFavoriteItems,
                includeFavoritePlayers);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(
                out includeOverriddenDisplayName,
                out includeCharacterName,
                out includeFavoriteItems,
                out includeFavoritePlayers);
        }
    }
}
