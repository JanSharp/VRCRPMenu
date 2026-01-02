using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestsPlayerLeaveHandler : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private GMRequest[] toMark = new GMRequest[ArrList.MinCapacity];
        private int toMarkCount = 0;

        private void MarkAllAsReadRequestedByPlayer(CorePlayerData player)
        {
            GMRequest[] requests = requestsManager.ActiveRequestsRaw;
            int count = requestsManager.ActiveRequestsCount;
            // Find them all first because marking raises events.
            for (int i = 0; i < count; i++)
            {
                GMRequest request = requests[i];
                if (request.requestingCorePlayer == player)
                    ArrList.Add(ref toMark, ref toMarkCount, request);
            }
            for (int i = 0; i < toMarkCount; i++)
                requestsManager.MarkReadInGS(toMark[i], null);
            ArrList.Clear(ref toMark, ref toMarkCount);
        }

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataDeleted)]
        public void OnPlayerDataDeleted() => MarkAllAsReadRequestedByPlayer(playerDataManager.PlayerDataForEvent);

        [PlayerDataEvent(PlayerDataEventType.OnPlayerDataWentOffline)]
        public void OnPlayerDataWentOffline() => MarkAllAsReadRequestedByPlayer(playerDataManager.PlayerDataForEvent);
    }
}
