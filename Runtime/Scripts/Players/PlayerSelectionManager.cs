using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionManager : DynamicDataManager
    {
        public override string GameStateInternalName => "jansharp.rp-menu-player-selection";
        public override string GameStateDisplayName => "Player Selection";
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;

        public override string DynamicDataClassName => nameof(PlayerSelectionGroup);
    }
}
