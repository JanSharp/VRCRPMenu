using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuDefaultPageSettings : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuSettingsManagerAPI menuSettingsManager;

        [SerializeField] private MenuDefaultPageSettingToggle[] toggles;

        private void MakeTogglesMatchLatencyState()
        {
            RPMenuDefaultPageType defaultPage = menuSettingsManager.LatencyDefaultPage;
            foreach (MenuDefaultPageSettingToggle toggle in toggles)
                toggle.toggle.SetIsOnWithoutNotify(toggle.defaultPageType == defaultPage);
        }

        public void OnValueChanged(MenuDefaultPageSettingToggle toggle)
        {
            if (toggle.toggle.isOn)
                menuSettingsManager.SendSetDefaultPageIA(menuSettingsManager.LocalPlayerSettings, toggle.defaultPageType);
            else
                MakeTogglesMatchLatencyState();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit() => MakeTogglesMatchLatencyState();

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp() => MakeTogglesMatchLatencyState();

        [MenuSettingsEvent(MenuSettingsEventType.OnLocalLatencyDefaultPageSettingChanged)]
        public void OnLocalLatencyDefaultPageSettingChanged() => MakeTogglesMatchLatencyState();
    }
}
