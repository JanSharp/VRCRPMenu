using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ToggleGroupWithFloatValues : UdonSharpBehaviour
    {
        public Toggle[] toggles;
        public float[] values;

        public float GetValue()
        {
            int length = toggles.Length;
            for (int i = 0; i < length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle.isOn && toggle.gameObject.activeSelf)
                    return values[i];
            }
            return float.NaN; // We love NaN infecting everything.
        }
    }
}
