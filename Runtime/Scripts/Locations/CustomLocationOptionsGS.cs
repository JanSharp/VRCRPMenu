using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CustomLocationOptionsGS : DynamicDataOptionsGS
    {
        public override string GameStateInternalName => "jansharp.rp-menu-custom-locations-options";
        public override string GameStateDisplayName => "Custom Locations Options";
    }
}
