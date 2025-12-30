using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestsPage : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;

        public GMRequestsList requestsList;

        private bool isInitialized = false;

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnInit");
#endif
            Initialize();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnClientBeginCatchUp");
#endif
            Initialize();
        }

        private void Initialize()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  Initialize");
#endif
            if (!lockstep.IsContinuationFromPrevFrame)
                requestsList.Initialize();
            requestsList.RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            isInitialized = true;
        }

        public void OnRespondClick(GMRequestRow row)
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnRespondClick");
#endif
            if (!row.request.latencyIsRead)
                requestsManager.SendMarkReadIA(row.request);
            // TODO: Teleport.
        }

        public void OnJoinClick(GMRequestRow row)
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnJoinClick");
#endif
            // TODO: Teleport.
        }

        public void OnReadToggleValueChanged(GMRequestRow row)
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnReadToggleValueChanged");
#endif
            bool isOn = row.readToggle.isOn;
            if (row.request.latencyIsRead == isOn)
                return;
            if (isOn)
                requestsManager.SendMarkReadIA(row.request);
            else
                requestsManager.SendMarkUnreadIA(row.request);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreatedInLatency)]
        public void OnGMRequestCreatedInLatency()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestCreatedInLatency");
#endif
            if (!isInitialized)
                return;
            requestsList.CreateRow(requestsManager.RequestForEvent);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestCreated)]
        public void OnGMRequestCreated()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestCreated");
#endif
            if (!isInitialized)
                return;
            GMRequest request = requestsManager.RequestForEvent;
            if (requestsList.TryGetRow(request, out GMRequestRow row))
            {
                requestsList.UpdateRowExceptRequester(row);
                requestsList.ResortRow(row);
            }
            else
                requestsList.CreateRow(request);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestChangedInLatency");
#endif
            if (!isInitialized)
                return;
            if (requestsList.TryGetRow(requestsManager.RequestForEvent, out GMRequestRow row))
            {
                requestsList.UpdateRowExceptRequester(row);
                requestsList.ResortRow(row);
            }
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChanged)]
        public void OnGMRequestChanged()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestChanged");
#endif
            if (!isInitialized)
                return;
            if (requestsList.TryGetRow(requestsManager.RequestForEvent, out GMRequestRow row))
            {
                requestsList.UpdateRowExceptRequester(row);
                requestsList.ResortRow(row);
            }
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestDeletedInLatency");
#endif
            if (!isInitialized)
                return;
            if (requestsList.TryGetRow(requestsManager.RequestForEvent, out GMRequestRow row))
                requestsList.RemoveRow(row);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestUnDeletedInLatency)]
        public void OnGMRequestUnDeletedInLatency()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestUnDeletedInLatency");
#endif
            if (!isInitialized)
                return;
            GMRequest request = requestsManager.RequestForEvent;
            if (requestsList.TryGetRow(request, out GMRequestRow row))
            {
                requestsList.UpdateRowExceptRequester(row);
                requestsList.ResortRow(row);
            }
            else
                requestsList.CreateRow(request);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeleted)]
        public void OnGMRequestDeleted()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestDeleted");
#endif
            if (!isInitialized)
                return;
            if (requestsList.TryGetRow(requestsManager.RequestForEvent, out GMRequestRow row))
                requestsList.RemoveRow(row);
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataOverriddenDisplayNameChanged)]
        public void OnRPPlayerDataOverriddenDisplayNameChanged()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnRPPlayerDataOverriddenDisplayNameChanged");
#endif
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            GMRequestRow[] rows = requestsList.Rows;
            int count = requestsList.RowsCount;
            for (int i = 0; i < count; i++)
            {
                GMRequestRow row = rows[i];
                GMRequest request = row.request;
                if (request.latencyRespondingPlayer == rpPlayerData)
                    requestsList.UpdateRowResponder(row);
                if (request.requestingPlayer == rpPlayerData)
                    requestsList.UpdateRowRequester(row);
            }
        }

        [PlayersBackendEvent(PlayersBackendEventType.OnRPPlayerDataCharacterNameChanged)]
        public void OnRPPlayerDataCharacterNameChanged()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnRPPlayerDataCharacterNameChanged");
#endif
            RPPlayerData rpPlayerData = playersBackendManager.RPPlayerDataForEvent;
            GMRequestRow[] rows = requestsList.Rows;
            int count = requestsList.RowsCount;
            for (int i = 0; i < count; i++)
            {
                GMRequestRow row = rows[i];
                GMRequest request = row.request;
                if (request.requestingPlayer == rpPlayerData)
                    requestsList.UpdateRowRequester(row);
            }
        }
    }
}
