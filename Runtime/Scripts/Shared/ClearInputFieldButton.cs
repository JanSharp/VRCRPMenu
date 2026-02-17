using TMPro;
using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ClearInputFieldButton : UdonSharpBehaviour
    {
        public TMP_InputField inputField;
        public Button clearButton;
        public Selectable clearButtonIcon;

        public void OnInputFieldValueChanged()
        {
            UpdateInteractable();
        }

        public void OnClearClick()
        {
            inputField.text = "";
        }

        private void UpdateInteractable()
        {
            bool interactable = inputField.text != "";
            clearButton.interactable = interactable;
            clearButtonIcon.interactable = interactable;
        }
    }
}
