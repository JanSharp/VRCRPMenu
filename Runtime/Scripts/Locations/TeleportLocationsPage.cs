using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeleportLocationsPage : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private TeleportLocationsManagerAPI teleportLocationsManager;
        [HideInInspector][SerializeField][FindInParent] private MenuPageRoot menuPageRoot;

        public TeleportLocationsList rowsList;

        private bool isInitialized = false;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            menuPageRoot.HideByDefault();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            rowsList.RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            rowsList.RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            isInitialized = true;
        }

        #region RowsManagement

        [TeleportLocationsEvent(TeleportLocationsEventType.OnLocationBecameShown)]
        public void OnLocationBecameShown()
        {
            menuPageRoot.IncrementShouldBeShown();
            if (!isInitialized)
                return;
            rowsList.CreateRow(teleportLocationsManager.TeleportLocationForEvent);
        }

        [TeleportLocationsEvent(TeleportLocationsEventType.OnLocationBecameHidden)]
        public void OnLocationBecameHidden()
        {
            menuPageRoot.DecrementShouldBeShown();
            if (!isInitialized)
                return;
            TeleportLocation location = teleportLocationsManager.TeleportLocationForEvent;
            if (!rowsList.TryGetRow(location.Order, out TeleportLocationRow row))
                return;
            rowsList.RemoveRow(row);
        }

        #endregion

        public void OnTeleportClick(TeleportLocationRow row)
        {
            if (row.location == null)
                return;
            Transform location = row.location.transform;
            teleportManager.TeleportTo(location.position, location.rotation, recordUndo: false);
        }
    }
}
