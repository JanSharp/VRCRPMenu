using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestsPage : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;

        public GMRequestsList requestsList;
        public LayerMask localPlayerCollidingLayers;

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
            GMRequest request = row.request;
            if (!request.latencyIsRead)
                requestsManager.SendMarkReadIA(request);
            TeleportToRequester(request);
        }

        public void OnJoinClick(GMRequestRow row)
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnJoinClick");
#endif
            TeleportToRequester(row.request);
        }

        private void TeleportToRequester(GMRequest request)
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  TeleportToRequester");
#endif
            if (request.requestingPlayer == null || request.requestingPlayer.core.isLocal)
                return;
            VRCPlayerApi player = request.requestingPlayer.core.playerApi;
            if (!Utilities.IsValid(player))
                return;
            Vector3 position = player.GetPosition() + Vector3.up * 0.15f;
            Quaternion rotation = player.GetRotation();
            float radius = LocalPlayerCapsule.GetRadius();
            if (Physics.CapsuleCast(
                position + Vector3.up * radius,
                position + Vector3.up * (LocalPlayerCapsule.GetHeight() - radius),
                radius,
                rotation * Vector3.forward,
                // out RaycastHit hit,
                maxDistance: 1f,
                localPlayerCollidingLayers,
                QueryTriggerInteraction.Ignore))
            {
                Networking.LocalPlayer.TeleportTo(position, rotation);
            }
            else
                Networking.LocalPlayer.TeleportTo(
                    position + rotation * Vector3.forward,
                    rotation * Quaternion.AngleAxis(180f, Vector3.up));
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
            if (!requestsList.TryGetRow(request, out GMRequestRow row))
                return; // Already deleted in latency again.
            requestsList.UpdateRowTimeInfo(row); // requestedAtTick is known now.
            requestsList.ResortRow(row);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestChangedInLatency)]
        public void OnGMRequestChangedInLatency()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestChangedInLatency");
#endif
            if (!isInitialized)
                return;
            if (!requestsList.TryGetRow(requestsManager.RequestForEvent, out GMRequestRow row))
                return;
            requestsList.UpdateRowExceptRequester(row);
            requestsList.ResortRow(row);
        }

        [GMRequestsEvent(GMRequestsEventType.OnGMRequestDeletedInLatency)]
        public void OnGMRequestDeletedInLatency()
        {
#if RP_MENU_DEBUG
            Debug.Log("[RPMenuDebug] GMRequestsPage  OnGMRequestDeletedInLatency");
#endif
            if (!isInitialized)
                return;
            if (!requestsList.TryGetRow(requestsManager.RequestForEvent, out GMRequestRow row))
                return;
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
            requestsList.CreateRow(requestsManager.RequestForEvent);
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

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            GMRequestRow[] rows = requestsList.Rows;
            int count = requestsList.RowsCount;
            for (int i = 0; i < count; i++)
            {
                GMRequestRow row = rows[i];
                requestsList.UpdateRowResponder(row);
                requestsList.UpdateRowRequester(row);
            }
        }
    }
}
