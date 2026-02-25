using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendImportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public bool includeOverriddenDisplayName = true;
        [System.NonSerialized] public bool includeCharacterName = true;
        [System.NonSerialized] public bool includeFavoriteItems = true;

        public override LockstepGameStateOptionsData Clone()
        {
            PlayersBackendImportOptions clone = WannaBeClasses.New<PlayersBackendImportOptions>(nameof(PlayersBackendImportOptions));
            clone.includeOverriddenDisplayName = includeOverriddenDisplayName;
            clone.includeCharacterName = includeCharacterName;
            clone.includeFavoriteItems = includeFavoriteItems;
            return clone;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(
                includeOverriddenDisplayName,
                includeCharacterName,
                includeFavoriteItems);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(
                out includeOverriddenDisplayName,
                out includeCharacterName,
                out includeFavoriteItems);
        }
    }
}
