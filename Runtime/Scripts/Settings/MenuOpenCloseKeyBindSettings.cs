using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuOpenCloseKeyBindSettings : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuSettingsManagerAPI menuSettingsManager;

        [SerializeField] private MenuOpenCloseKeyBindSettingToggle[] toggles;

        private void MakeTogglesMatchLatencyState()
        {
            MenuOpenCloseKeyBind keyBind = menuSettingsManager.LatencyMenuOpenCloseKeyBind;
            foreach (MenuOpenCloseKeyBindSettingToggle toggle in toggles)
                toggle.toggle.SetIsOnWithoutNotify(toggle.keyBind == keyBind);
        }

        public void OnValueChanged(MenuOpenCloseKeyBindSettingToggle toggle)
        {
            if (toggle.toggle.isOn)
                menuSettingsManager.SendSetOpenCloseKeyBindIA(menuSettingsManager.LocalPlayerSettings, toggle.keyBind);
            else
                MakeTogglesMatchLatencyState();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeTogglesMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeTogglesMatchLatencyState();

        [MenuSettingsEvent(MenuSettingsEventType.OnLocalLatencyOpenCloseKeyBindSettingChanged)]
        public void OnLocalLatencyOpenCloseKeyBindSettingChanged() => MakeTogglesMatchLatencyState();
    }
}
