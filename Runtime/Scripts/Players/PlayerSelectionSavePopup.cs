using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionSavePopup : DynamicDataSavePopup
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;
        protected override DynamicDataManager GetDynamicDataManager() => selectionManager;

        protected override void PopulateDataForAdd(DynamicData data)
        {
            PlayerSelectionGroup group = (PlayerSelectionGroup)data;
            CorePlayerData[] players = new CorePlayerData[selectionManager.selectedPlayersCount];
            System.Array.Copy(selectionManager.selectedPlayers, players, players.Length);
            group.selectedPlayers = players;
        }

        protected override void PopulateDataForOverwrite(DynamicData data, DynamicData toOverwrite)
        {
            PopulateDataForAdd(data);
        }

        protected override void PopulateDataForUndoOverwrite(DynamicData data, DynamicData toRestore)
        {
            PlayerSelectionGroup group = (PlayerSelectionGroup)data;
            CorePlayerData[] selectedPlayers = ((PlayerSelectionGroup)toRestore).selectedPlayers;
            int nullCount = 0;
            foreach (CorePlayerData player in selectedPlayers)
                if (player == null || player.isDeleted)
                    nullCount++;
            if (nullCount == 0)
                group.selectedPlayers = selectedPlayers;
            else
            {
                CorePlayerData[] newPlayers = new CorePlayerData[selectedPlayers.Length - nullCount];
                int i = 0;
                foreach (CorePlayerData player in selectedPlayers)
                    if (player != null && !player.isDeleted)
                        newPlayers[i++] = player;
                group.selectedPlayers = newPlayers;
            }
        }

        protected override string DefaultBaseDataName => "Group";

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupAdded)]
        public void OnSelectionGroupAdded()
        {
            OnDynamicDataAdded(selectionManager.SelectionGroupForEvent);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupPlayerRemoved)]
        public void OnSelectionGroupPlayerRemoved()
        {
            if (TryGetDynamicDataButton(selectionManager.SelectionGroupForEvent.id, out DynamicDataOverwriteButton button))
                UpdateDynamicDataButtonLabel(button);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupOverwritten)]
        public void OnSelectionGroupOverwritten()
        {
            OnDynamicDataOverwritten(selectionManager.SelectionGroupForEvent, selectionManager.OverwrittenSelectionGroupForEvent);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnSelectionGroupUndoOverwriteStackChanged)]
        public void OnSelectionGroupUndoOverwriteStackChanged()
        {
            OnDynamicDataUndoOverwriteStackChanged();
        }

        protected override string GetDynamicDataLabel(DynamicData data)
        {
            PlayerSelectionGroup group = (PlayerSelectionGroup)data;
            return $"{base.GetDynamicDataLabel(data)} ({group.selectedPlayers.Length})";
        }
    }
}
