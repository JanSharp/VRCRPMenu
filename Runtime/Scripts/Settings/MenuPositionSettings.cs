using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuPositionSettings : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuSettingsManagerAPI menuSettingsManager;

        [SerializeField] private MenuPositionSettingToggle[] toggles;

        private void MakeTogglesMatchLatencyState()
        {
            MenuPositionType menuPosition = menuSettingsManager.LatencyMenuPosition;
            foreach (MenuPositionSettingToggle toggle in toggles)
                toggle.toggle.SetIsOnWithoutNotify(toggle.menuPosition == menuPosition);
        }

        public void OnValueChanged(MenuPositionSettingToggle toggle)
        {
            if (toggle.toggle.isOn)
                menuSettingsManager.SendSetMenuPositionIA(menuSettingsManager.LocalPlayerSettings, toggle.menuPosition);
            else
                MakeTogglesMatchLatencyState();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeTogglesMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeTogglesMatchLatencyState();

        [MenuSettingsEvent(MenuSettingsEventType.OnLocalLatencyMenuPositionSettingChanged)]
        public void OnLocalLatencyMenuPositionSettingChanged() => MakeTogglesMatchLatencyState();
    }
}
