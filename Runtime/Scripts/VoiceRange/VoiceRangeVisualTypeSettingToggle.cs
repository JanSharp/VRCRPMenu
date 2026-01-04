using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeVisualTypeSettingToggle : UdonSharpBehaviour
    {
        public VoiceRangeVisualTypeSettings settings;
        public Toggle toggle;
        public VoiceRangeVisualizationType visualType;

        public void OnValueChanged() => settings.OnValueChanged(this);
    }
}
