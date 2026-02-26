using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionOptionsGS : DynamicDataOptionsGS
    {
        public override string GameStateInternalName => "jansharp.rp-menu-player-selection-options";
        public override string GameStateDisplayName => "Player Selection Options";
    }
}
