using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsFlyingType : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;

        [SerializeField] private NoClipSettingsFlyingTypeToggle[] toggles;

        private void MakeTogglesMatchLatencyState()
        {
            NoClipFlyingType flyingType = noClipSettingsManager.LatencyNoClipFlyingType;
            foreach (NoClipSettingsFlyingTypeToggle toggle in toggles)
                toggle.SetIsOnWithoutNotify(toggle.flyingType == flyingType);
        }

        public void OnValueChanged(NoClipSettingsFlyingTypeToggle toggle)
        {
            if (toggle.IsOn)
                noClipSettingsManager.SendSetNoClipFlyingTypeIA(noClipSettingsManager.LocalNoClipSettingsPlayerData, toggle.flyingType);
            else
                toggle.SetIsOnWithoutNotify(true);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeTogglesMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeTogglesMatchLatencyState();

        [NoClipSettingsEvent(NoClipSettingsEventType.OnLocalLatencyNoClipFlyingTypeChanged)]
        public void OnLocalLatencyNoClipFlyingTypeChanged() => MakeTogglesMatchLatencyState();
    }
}
