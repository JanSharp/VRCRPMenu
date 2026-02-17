using TMPro;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Keyboard : UdonSharpBehaviour
    {
        public TMP_InputField inputField;
        [Min(0f)]
        [Tooltip("TMP_InputField characterLimit is not exposed to Udon.")]
        public int characterLimit;

        public void OnClick(KeyboardKey key)
        {
            string text = inputField.text;
            int length = text.Length;
            if (key.isBackspace)
            {
                if (length != 0)
                    inputField.text = text.Substring(0, length - 1);
                return;
            }
            if (characterLimit != 0 && length >= characterLimit)
                return;
            inputField.text = text + key.character;
        }
    }
}
