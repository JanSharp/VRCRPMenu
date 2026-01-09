using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionSelectNoneButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;
        public Button button;
        public Selectable label;

        public void OnClick()
        {
            selectionManager.SelectNone();
        }

        private void UpdateInteractable()
        {
            bool interactable = selectionManager.selectedPlayersCount != 0;
            button.interactable = interactable;
            label.interactable = interactable;
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnOnePlayerSelectionChanged)]
        public void OnOnePlayerSelectionChanged() => UpdateInteractable();

        [PlayerSelectionEvent(PlayerSelectionEventType.OnMultiplePlayerSelectionChanged)]
        public void OnMultiplePlayerSelectionChanged() => UpdateInteractable();
    }
}
