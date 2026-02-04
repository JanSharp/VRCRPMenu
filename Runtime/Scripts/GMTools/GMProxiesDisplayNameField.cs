using TMPro;
using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMProxiesDisplayNameField : UdonSharpBehaviour
    {
        public TMP_InputField displayNameField;
        public Button clearButton;
        public Selectable clearButtonIcon;

        public void OnDisplayNameValueChanged()
        {
            UpdateInteractable();
        }

        public void OnClearClick()
        {
            displayNameField.SetTextWithoutNotify("");
            UpdateInteractable();
        }

        private void UpdateInteractable()
        {
            bool interactable = displayNameField.text != "";
            clearButton.interactable = interactable;
            clearButtonIcon.interactable = interactable;
        }
    }
}
