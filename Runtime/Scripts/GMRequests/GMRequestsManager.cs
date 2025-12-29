using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [LockstepGameStateDependency(typeof(PlayerDataManagerAPI))]
    [CustomRaisedEventsDispatcher(typeof(GMRequestsEventAttribute), typeof(GMRequestsEventType))]
    public class GMRequestsManager : GMRequestsManagerAPI
    {
        public override string GameStateInternalName => "rp-menu.gm-requests";
        public override string GameStateDisplayName => "GM Requests";
        public override bool GameStateSupportsImportExport => false;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;
        public override LockstepGameStateOptionsUI ExportUI => null;
        public override LockstepGameStateOptionsUI ImportUI => null;

        [HideInInspector][SerializeField][SingletonReference] private WannaBeClassesManager wannaBeClasses;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;

        private const uint MinLiveTicksWhenMarkedRead = (uint)(60f * LockstepAPI.TickRate);
        private const uint MinTotalLiveTicks = (uint)(10f * 60f * LockstepAPI.TickRate);

        #region LatencyState
        /// <summary>
        /// <para>Once a request is part of the game state, accessing it through here is also game state
        /// save.</para>
        /// <para><see cref="ulong"/> uniqueId => <see cref="GMRequest"/> request</para>
        /// </summary>
        private DataDictionary requestsByUniqueId = new DataDictionary();
        #endregion

        #region GameState
        private GMRequest[] requests = new GMRequest[ArrList.MinCapacity];
        private int requestsCount = 0;
        /// <summary>
        /// <para><see cref="uint"/> id => <see cref="GMRequest"/> request</para>
        /// </summary>
        private DataDictionary requestsById = new DataDictionary();
        /// <summary>
        /// <para><c>0u</c> is an invalid id.</para>
        /// </summary>
        private uint nextRequestId = 1u;
        #endregion

        #region Permissions

        [PermissionDefinitionReference(nameof(requestGMPermissionDef))]
        public string requestGMPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMPermissionDef;

        [PermissionDefinitionReference(nameof(requestGMUrgentlyPermissionDef))]
        public string requestGMUrgentlyPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition requestGMUrgentlyPermissionDef;

        [PermissionDefinitionReference(nameof(viewAndEditGMRequestsPermissionDef))]
        public string viewAndEditGMRequestsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewAndEditGMRequestsPermissionDef;

        private bool HasCreatePermission(CorePlayerData actingPlayer, CorePlayerData requestingPlayer, GMRequestType requestType)
        {
            if (actingPlayer != requestingPlayer)
                return false;
            switch (requestType)
            {
                case GMRequestType.Regular:
                    return permissionManager.PlayerHasPermission(actingPlayer, requestGMPermissionDef);
                case GMRequestType.Urgent:
                    return permissionManager.PlayerHasPermission(actingPlayer, requestGMUrgentlyPermissionDef);
                default:
                    return false;
            }
        }

        private bool HasSetRequestTypePermission(CorePlayerData actingPlayer, CorePlayerData requestingPlayer, GMRequestType requestType)
        {
            return HasCreatePermission(actingPlayer, requestingPlayer, requestType);
        }

        private bool HasEditPermission(CorePlayerData actingPlayer)
        {
            return permissionManager.PlayerHasPermission(actingPlayer, viewAndEditGMRequestsPermissionDef);
        }

        private bool HasDeletePermission(CorePlayerData actingPlayer, GMRequest request)
        {
            return request.requestingPlayer == null || request.requestingPlayer == actingPlayer;
        }

        #endregion

        private uint localPlayerId;
        private CorePlayerData localPlayer;

        private void Start()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            FetchLocalPlayerData();
        }

        private void FetchLocalPlayerData()
        {
            localPlayer = playerDataManager.GetCorePlayerDataForPlayerId(localPlayerId);
        }

        private GMRequest CreateNewGMRequestInst()
        {
            return wannaBeClasses.New<GMRequest>(nameof(GMRequest));
        }

        #region InputActions

        public override void SendCreateIA(GMRequestType requestType)
        {
            if (!lockstep.IsInitialized)
                return;
            lockstep.WriteByte((byte)requestType);
            // Cannot rely on lockstep.SendingPlayerId, player could leave.
            playerDataManager.WriteCorePlayerDataRef(localPlayer);
            ulong uniqueId = lockstep.SendInputAction(createIAId);
            CreateInLS(uniqueId, requestType, localPlayer);
        }

        public void CreateInLS(ulong uniqueId, GMRequestType requestType, CorePlayerData requestingPlayer)
        {
            GMRequest request = CreateNewGMRequestInst();
            request.isLatency = true;
            request.uniqueId = uniqueId;
            request.requestingPlayer = requestingPlayer;
            request.latencyRequestType = requestType;
            requestsByUniqueId.Add(uniqueId, request);
            RaiseOnGMRequestCreatedInLatency(request);
        }

        [HideInInspector][SerializeField] private uint createIAId;
        [LockstepInputAction(nameof(createIAId))]
        public void OnCreateIA()
        {
            GMRequestType requestType = (GMRequestType)lockstep.ReadByte();
            CorePlayerData requestingPlayer = playerDataManager.ReadCorePlayerDataRef();
            bool hasPermission = HasCreatePermission(playerDataManager.SendingPlayerData, requestingPlayer, requestType);

            GMRequest request;

            ulong uniqueId = lockstep.SendingUniqueId;
            if (requestsByUniqueId.TryGetValue(uniqueId, out DataToken requestToken))
            {
                request = (GMRequest)requestToken.Reference;
                if (!hasPermission)
                {
                    request.latencyIsDeleted = true;
                    requestsByUniqueId.Remove(uniqueId);
                    RaiseOnGMRequestDeletedInLatency(request);
                    request.DecrementRefsCount();
                    return;
                }
                request.isLatency = false;
            }
            else
            {
                if (!hasPermission)
                    return;
                request = CreateNewGMRequestInst();
                request.uniqueId = uniqueId;
                request.latencyRequestType = requestType;
                requestsByUniqueId.Add(uniqueId, request);
            }

            request.id = nextRequestId++;
            requestsById.Add(request.id, request);
            request.index = requestsCount;
            ArrList.Add(ref requests, ref requestsCount, request);

            request.requestType = requestType;
            request.requestedAtTick = lockstep.CurrentTick;
            request.requestingPlayer = requestingPlayer; // Overwrite even if it was latency hidden.
            RaiseOnGMRequestCreated(request);
        }

        public override void SendSetRequestTypeIA(GMRequest request, GMRequestType requestType)
        {
            if (!lockstep.IsInitialized)
                return;
            WriteGMRequestRef(request);
            lockstep.WriteByte((byte)requestType);
            request.latencyHiddenUniqueIds.Add(lockstep.SendInputAction(setRequestTypeIAId), true);
            request.latencyRequestType = requestType;
            RaiseOnGMRequestChangedInLatency(request);
        }

        [HideInInspector][SerializeField] private uint setRequestTypeIAId;
        [LockstepInputAction(nameof(setRequestTypeIAId))]
        public void OnSetRequestTypeIA()
        {
            GMRequest request = ReadGMRequestRef();
            if (request == null)
                return;
            GMRequestType requestType = (GMRequestType)lockstep.ReadByte();
            if (!HasSetRequestTypePermission(playerDataManager.SendingPlayerData, request.requestingPlayer, requestType))
            {
                if (request.latencyHiddenUniqueIds.Count == 0)
                    return;
                request.latencyHiddenUniqueIds.Clear(); // Latency state has predicted incorrectly.
                request.latencyRequestType = request.requestType;
                RaiseOnGMRequestChangedInLatency(request);
                return;
            }
            request.requestType = requestType;
            if (request.latencyHiddenUniqueIds.Remove(lockstep.SendingUniqueId))
            {
                RaiseOnGMRequestChanged(request);
                return;
            }
            request.latencyHiddenUniqueIds.Clear(); // Latency state may have predicted incorrectly.
            request.latencyRequestType = request.requestType;
            RaiseOnGMRequestChanged(request);
        }

        public override void SendMarkReadIA(GMRequest request)
        {
            if (!lockstep.IsInitialized)
                return;
            WriteGMRequestRef(request);
            playerDataManager.WriteCorePlayerDataRef(localPlayer);
            request.latencyHiddenUniqueIds.Add(lockstep.SendInputAction(markReadIAId), true);
            request.latencyIsRead = true;
            request.latencyRespondingPlayer = localPlayer;
            RaiseOnGMRequestChangedInLatency(request);
        }

        private bool ResetIsReadIfMissingPermission(CorePlayerData actingPlayer, GMRequest request)
        {
            if (HasEditPermission(actingPlayer))
                return false;
            if (request.latencyHiddenUniqueIds.Count == 0)
                return true;
            request.latencyHiddenUniqueIds.Clear(); // Latency state has predicted incorrectly.
            request.latencyIsRead = request.isRead;
            request.latencyRespondingPlayer = request.respondingPlayer;
            RaiseOnGMRequestChangedInLatency(request);
            return true;
        }

        [HideInInspector][SerializeField] private uint markReadIAId;
        [LockstepInputAction(nameof(markReadIAId))]
        public void OnMarkReadIA()
        {
            GMRequest request = ReadGMRequestRef();
            if (request == null)
                return;
            if (ResetIsReadIfMissingPermission(playerDataManager.SendingPlayerData, request))
                return;
            MarkAsRead(request, playerDataManager.ReadCorePlayerDataRef());
            if (request.latencyHiddenUniqueIds.Remove(lockstep.SendingUniqueId))
            {
                RaiseOnGMRequestChanged(request);
                return;
            }
            request.latencyHiddenUniqueIds.Clear(); // Latency state may have predicted incorrectly.
            request.latencyIsRead = true;
            request.latencyRespondingPlayer = request.respondingPlayer;
            RaiseOnGMRequestChanged(request);
        }

        public override void SendMarkUnreadIA(GMRequest request)
        {
            if (!lockstep.IsInitialized)
                return;
            WriteGMRequestRef(request);
            request.latencyHiddenUniqueIds.Add(lockstep.SendInputAction(markUnreadIAId), true);
            request.latencyIsRead = false;
            request.latencyRespondingPlayer = null;
            RaiseOnGMRequestChangedInLatency(request);
        }

        [HideInInspector][SerializeField] private uint markUnreadIAId;
        [LockstepInputAction(nameof(markUnreadIAId))]
        public void OnMarkUnreadIA()
        {
            GMRequest request = ReadGMRequestRef();
            if (request == null)
                return;
            if (ResetIsReadIfMissingPermission(playerDataManager.SendingPlayerData, request))
                return;
            MarkAsUnread(request);
            if (request.latencyHiddenUniqueIds.Remove(lockstep.SendingUniqueId))
            {
                RaiseOnGMRequestChanged(request);
                return;
            }
            request.latencyHiddenUniqueIds.Clear(); // Latency state may have predicted incorrectly.
            request.latencyIsRead = false;
            request.latencyRespondingPlayer = null;
            RaiseOnGMRequestChanged(request);
        }

        public override void SendDeleteIA(GMRequest request)
        {
            if (!lockstep.IsInitialized)
                return;
            WriteGMRequestRef(request);
            lockstep.SendInputAction(deleteIAId);
            request.latencyIsDeleted = true;
            RaiseOnGMRequestDeletedInLatency(request);
        }

        [HideInInspector][SerializeField] private uint deleteIAId;
        [LockstepInputAction(nameof(deleteIAId))]
        public void OnDeleteIA()
        {
            GMRequest request = ReadGMRequestRef();
            if (request == null)
                return;
            if (!HasDeletePermission(playerDataManager.SendingPlayerData, request))
            {
                if (!request.latencyIsDeleted)
                    return;
                request.latencyIsDeleted = false;
                RaiseOnGMRequestUnDeletedInLatency(request);
                return;
            }
            DeleteGMRequestInGS(request);
        }

        private void DeleteGMRequestInGS(GMRequest request)
        {
            request.isDeleted = false;
            request.latencyIsDeleted = true;
            request.latencyHiddenUniqueIds.Clear();

            int index = request.index;
            requestsCount--;
            if (requestsCount != index)
            {
                GMRequest top = requests[requestsCount];
                requests[index] = top;
                top.index = index;
            }
            requestsById.Remove(request.id);
            requestsByUniqueId.Remove(request.uniqueId);
            RaiseOnGMRequestDeleted(request);
            request.DecrementRefsCount();
        }

        #endregion

        #region AutoDeletion

        private void MarkAsRead(GMRequest request, CorePlayerData respondingPlayer)
        {
            uint currentTick = lockstep.CurrentTick;
            request.isRead = true;
            request.autoDeleteAtTick = System.Math.Max(
                currentTick + MinLiveTicksWhenMarkedRead,
                request.requestedAtTick + MinTotalLiveTicks);
            request.respondingPlayer = respondingPlayer;

            WriteGMRequestRef(request);
            lockstep.SendEventDelayedTicks(checkAutoDeletionIAId, request.autoDeleteAtTick - currentTick);
        }

        private void MarkAsUnread(GMRequest request)
        {
            request.isRead = false;
            request.autoDeleteAtTick = 0u;
            request.respondingPlayer = null;
            // Cannot cancel the delayed event but it is fine, it has its own checks.
        }

        [HideInInspector][SerializeField] private uint checkAutoDeletionIAId;
        [LockstepInputAction(nameof(checkAutoDeletionIAId))]
        public void OnCheckAutoDeletionIA()
        {
            GMRequest request = ReadGMRequestRef();
            if (request == null || !request.isRead || request.autoDeleteAtTick != lockstep.CurrentTick)
                return;
            DeleteGMRequestInGS(request);
        }

        #endregion

        #region Utils

        public void WriteGMRequestRef(GMRequest request)
        {
            if (!request.isLatency)
            {
                lockstep.WriteSmallUInt(request.id);
                return;
            }
            lockstep.WriteByte(0xff); // WriteSmall never writes 0xff as its first byte.
            lockstep.WriteULong(request.uniqueId);
        }

        public GMRequest ReadGMRequestRef()
        {
            byte header = lockstep.ReadByte();
            if (header != 0xff)
            {
                lockstep.ReadStreamPosition--;
                uint id = lockstep.ReadSmallUInt();
                return GetGMRequestById(id);
            }
            ulong uniqueId = lockstep.ReadULong();
            return requestsByUniqueId.TryGetValue(uniqueId, out DataToken requestToken)
                ? (GMRequest)requestToken.Reference
                : null;
        }

        public GMRequest GetGMRequestById(uint id)
        {
            return requestsById.TryGetValue(id, out DataToken requestToken)
                ? (GMRequest)requestToken.Reference
                : null;
        }

        #endregion

        #region Serialization

        private void WriteGMRequest(GMRequest request)
        {
            lockstep.WriteULong(request.uniqueId);
            lockstep.WriteSmallUInt(request.id);
            lockstep.WriteByte((byte)request.requestType);
            // Keep this as a separate flag in order to keep the option for requests to be read
            // without a player being associated as the responder
            lockstep.WriteFlags(request.isRead);
            lockstep.WriteSmallUInt(request.requestedAtTick);
            if (request.isRead)
                lockstep.WriteSmallUInt(request.autoDeleteAtTick);
            playerDataManager.WriteCorePlayerDataRef(request.requestingPlayer);
            playerDataManager.WriteCorePlayerDataRef(request.respondingPlayer);
        }

        private GMRequest ReadGMRequest(GMRequest request)
        {
            request.uniqueId = lockstep.ReadULong();
            request.id = lockstep.ReadSmallUInt();
            request.requestType = (GMRequestType)lockstep.ReadByte();
            lockstep.ReadFlags(out request.isRead);
            request.requestedAtTick = lockstep.ReadSmallUInt();
            request.autoDeleteAtTick = request.isRead ? lockstep.ReadSmallUInt() : 0u;
            request.requestingPlayer = playerDataManager.ReadCorePlayerDataRef();
            request.respondingPlayer = playerDataManager.ReadCorePlayerDataRef();
            requestsByUniqueId.Add(request.uniqueId, request);
            requestsById.Add(request.id, request);
            return request;
        }

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
            lockstep.WriteSmallUInt((uint)requestsCount);
            for (int i = 0; i < requestsCount; i++)
                WriteGMRequest(requests[i]);
        }

        private int suspendedIndexInArray = 0;
        private System.Diagnostics.Stopwatch suspensionSw = new System.Diagnostics.Stopwatch();
        private const long MaxWorkMSPerFrame = 10L;

        private bool LogicIsRunningLong()
        {
            if (suspensionSw.ElapsedMilliseconds >= MaxWorkMSPerFrame)
            {
                lockstep.FlagToContinueNextFrame();
                return true;
            }
            return false;
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
            if (!lockstep.IsContinuationFromPrevFrame)
            {
                FetchLocalPlayerData();
                requestsCount = (int)lockstep.ReadSmallUInt();
                ArrList.EnsureCapacity(ref requests, requestsCount);
            }
            suspensionSw.Restart();
            while (suspendedIndexInArray < requestsCount)
            {
                if (LogicIsRunningLong())
                    return null;
                requests[suspendedIndexInArray] = wannaBeClasses.New<GMRequest>(nameof(GMRequest));
                suspendedIndexInArray++;
            }
            suspendedIndexInArray = 0;
            for (int i = 0; i < requestsCount; i++)
                ReadGMRequest(requests[i]);
            return null;
        }

        #endregion

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestCreatedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestCreatedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestChangedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestChangedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestDeletedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestUnDeletedInLatencyListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onGMRequestDeletedListeners;

        private GMRequest requestForEvent;
        public override GMRequest RequestForEvent => requestForEvent;

        private void RaiseOnGMRequestCreatedInLatency(GMRequest requestForEvent)
        {
            this.requestForEvent = requestForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestCreatedInLatencyListeners, nameof(GMRequestsEventType.OnGMRequestCreatedInLatency));
            this.requestForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnGMRequestCreated(GMRequest requestForEvent)
        {
            this.requestForEvent = requestForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestCreatedListeners, nameof(GMRequestsEventType.OnGMRequestCreated));
            this.requestForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnGMRequestChangedInLatency(GMRequest requestForEvent)
        {
            this.requestForEvent = requestForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestChangedInLatencyListeners, nameof(GMRequestsEventType.OnGMRequestChangedInLatency));
            this.requestForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnGMRequestChanged(GMRequest requestForEvent)
        {
            this.requestForEvent = requestForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestChangedListeners, nameof(GMRequestsEventType.OnGMRequestChanged));
            this.requestForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnGMRequestDeletedInLatency(GMRequest requestForEvent)
        {
            this.requestForEvent = requestForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestDeletedInLatencyListeners, nameof(GMRequestsEventType.OnGMRequestDeletedInLatency));
            this.requestForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnGMRequestUnDeletedInLatency(GMRequest requestForEvent)
        {
            this.requestForEvent = requestForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestUnDeletedInLatencyListeners, nameof(GMRequestsEventType.OnGMRequestUnDeletedInLatency));
            this.requestForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnGMRequestDeleted(GMRequest requestForEvent)
        {
            this.requestForEvent = requestForEvent;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onGMRequestDeletedListeners, nameof(GMRequestsEventType.OnGMRequestDeleted));
            this.requestForEvent = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
