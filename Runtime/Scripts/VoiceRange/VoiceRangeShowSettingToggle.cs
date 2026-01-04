using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeShowSettingToggle : UdonSharpBehaviour
    {
        public Toggle toggle;
        public string voiceRangeInternalName;
        [System.NonSerialized] public VoiceRangeDefinition resolvedDef;
    }
}
