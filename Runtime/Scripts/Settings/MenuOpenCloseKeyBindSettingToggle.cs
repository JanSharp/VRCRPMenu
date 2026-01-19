using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuOpenCloseKeyBindSettingToggle : UdonSharpBehaviour
    {
        public MenuOpenCloseKeyBindSettings settings;
        public Toggle toggle;
        public MenuOpenCloseKeyBind keyBind;

        public void OnValueChanged() => settings.OnValueChanged(this);
    }
}
