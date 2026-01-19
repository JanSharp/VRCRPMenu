using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuPositionSettingToggle : UdonSharpBehaviour
    {
        public MenuPositionSettings settings;
        public Toggle toggle;
        public MenuPositionType menuPosition;

        public void OnValueChanged() => settings.OnValueChanged(this);
    }
}
