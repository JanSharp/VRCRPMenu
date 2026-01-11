using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionLoadPopup : DynamicDataLoadPopup
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;
        protected override DynamicDataManager GetDynamicDataManager() => selectionManager;

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupAdded)]
        public void OnSelectionGroupAdded()
        {
            OnDynamicDataAdded(selectionManager.SelectionGroupForEvent);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupPlayerRemoved)]
        public void OnSelectionGroupPlayerRemoved()
        {
            if (TryGetDynamicDataButton(selectionManager.SelectionGroupForEvent.id, out DynamicDataPopupListButton button))
                UpdateDynamicDataButtonLabel(button);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupOverwritten)]
        public void OnSelectionGroupOverwritten()
        {
            OnDynamicDataOverwritten(selectionManager.SelectionGroupForEvent, selectionManager.OverwrittenSelectionGroupForEvent);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupDeleted)]
        public void OnSelectionGroupDeleted()
        {
            OnDynamicDataDeleted(selectionManager.SelectionGroupForEvent);
        }

        protected override string GetDynamicDataLabel(DynamicData data)
        {
            PlayerSelectionGroup group = (PlayerSelectionGroup)data;
            return $"{base.GetDynamicDataLabel(data)} ({group.selectedPlayers.Length})";
        }
    }
}
