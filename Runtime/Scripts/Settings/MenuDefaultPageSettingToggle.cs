using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuDefaultPageSettingToggle : UdonSharpBehaviour
    {
        public MenuDefaultPageSettings settings;
        public Toggle toggle;
        public RPMenuDefaultPageType defaultPageType;

        public void OnValueChanged() => settings.OnValueChanged(this);
    }
}
