using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PerPlayerSelectionData : PerPlayerDynamicData
    {
        public override string PlayerDataInternalName => "jansharp.player-selection-data";
        public override string PlayerDataDisplayName => "Player Selection Data";
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        public override string DynamicDataClassName => nameof(PlayerSelectionGroup);
    }
}
