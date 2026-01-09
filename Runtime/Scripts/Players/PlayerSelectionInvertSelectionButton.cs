using TMPro;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionInvertSelectionButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;
        public string labelTextWhenNoneAreSelected = "Select All";
        public string labelTextOtherwise = "Invert Sel";
        public TextMeshProUGUI label;

        public void OnClick()
        {
            selectionManager.InvertSelection();
        }

        private void UpdateLabel()
        {
            label.text = selectionManager.selectedPlayersCount == 0
                ? labelTextWhenNoneAreSelected
                : labelTextOtherwise;
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnOnePlayerSelectionChanged)]
        public void OnOnePlayerSelectionChanged() => UpdateLabel();

        [PlayerSelectionEvent(PlayerSelectionEventType.OnMultiplePlayerSelectionChanged)]
        public void OnMultiplePlayerSelectionChanged() => UpdateLabel();
    }
}
