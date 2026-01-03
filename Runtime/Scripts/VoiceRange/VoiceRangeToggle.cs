using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeToggle : UdonSharpBehaviour
    {
        public VoiceRangeToggleGroup toggleGroupScript;
        public Toggle toggle;
        public string voiceRangeInternalName;

        public void OnValueChanged()
        {
            toggleGroupScript.OnValueChanged(this);
        }
    }
}
