using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PerPlayerSelectionData : PerPlayerDynamicData
    {
        public override string PlayerDataInternalName => "jansharp.rp-menu-player-selection";
        public override string PlayerDataDisplayName => "Player Selection";

        [HideInInspector][SerializeField][SingletonReference] protected PlayerSelectionManager selectionManager;
        public override DynamicDataManager DataManager => selectionManager;

        public override string DynamicDataClassName => nameof(PlayerSelectionGroup);
    }
}
