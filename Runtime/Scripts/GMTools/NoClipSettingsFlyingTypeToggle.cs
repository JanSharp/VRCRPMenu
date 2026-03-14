using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsFlyingTypeToggle : UdonSharpBehaviour
    {
        public NoClipSettingsFlyingType settings;
        public Toggle toggle;
        public bool isNone;
        public NoClipFlyingType flyingType;

        public void OnValueChanged() => settings.OnValueChanged(this);
    }
}
