using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestsList : SortableScrollableList
    {
        [HideInInspector][SerializeField][SingletonReference] private GMRequestsManagerAPI requestsManager;

        /// <summary>
        /// <para><see cref="ulong"/> requestUniqueId => <see cref="GMRequestRow"/> row</para>
        /// </summary>
        private DataDictionary rowsByUniqueId = new DataDictionary();
        public GMRequestRow[] Rows => (GMRequestRow[])rows;
        public int RowsCount => rowsCount;

        private uint nextTimeInfoUpdateTick;

        public override void Initialize()
        {
            base.Initialize();

            currentSortOrderFunction = nameof(CompareRow);
            someRowsAreOutOfSortOrder = false;
        }

        protected override void OnPageWentVisible()
        {
            nextTimeInfoUpdateTick = lockstep.CurrentTick;
            base.OnPageWentVisible();
        }

        protected override void OnPageUpdateWhileVisible()
        {
            base.OnPageUpdateWhileVisible();
            uint currentTick = lockstep.CurrentTick;
            if (currentTick < nextTimeInfoUpdateTick)
                return;
            uint ticksInASecond = (uint)LockstepAPI.TickRate;
            while (nextTimeInfoUpdateTick <= currentTick)
                nextTimeInfoUpdateTick += ticksInASecond;
            // "nextTimeInfoUpdateTick = currentTick + ticksInASecond" has the issue that it can drift
            // slightly causing the UI to appear as though timers did not change for up to 1.9 seconds.
            for (int i = 0; i < rowsCount; i++)
                UpdateRowTimeInfo((GMRequestRow)rows[i]);
        }

        #region RowsManagement

        public bool TryGetRow(GMRequest request, out GMRequestRow row)
        {
            if (rowsByUniqueId.TryGetValue(request.uniqueId, out DataToken rowToken))
            {
                row = (GMRequestRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        public GMRequestRow CreateRow(GMRequest request)
        {
            GMRequestRow row = CreateRowForRequest(request);
            rowsByUniqueId.Add(request.uniqueId, row);
            InsertSortNewRow(row);
            return row;
        }

        public void RemoveRow(GMRequestRow row)
        {
            rowsByUniqueId.Remove(row.request.uniqueId);
            RemoveRow((SortableScrollableRow)row);
        }

        public void RebuildRows() => RebuildRows(requestsManager.GMRequestsCount);

        protected override void OnRowCreated(SortableScrollableRow row) { }

        protected override void OnPreRebuildRows()
        {
            rowsByUniqueId.Clear();
        }

        protected override SortableScrollableRow RebuildRow(int index)
        {
            GMRequest request = requestsManager.GetGMRequest(index);
            GMRequestRow row = CreateRowForRequest(request);
            rowsByUniqueId.Add(request.uniqueId, row);
            return row;
        }

        private GMRequestRow CreateRowForRequest(GMRequest request)
        {
            GMRequestRow row = (GMRequestRow)CreateRow();
            row.request = request;
            UpdateRowRequester(row);
            UpdateRowExceptRequester(row);
            return row;
        }

        public void UpdateRowRequester(GMRequestRow row)
        {
            RPPlayerData requester = row.request.requestingPlayer;
            row.requesterText.text = requester == null
                ? "<Unknown>"
                : requester.characterName == ""
                    ? $"[{requester.PlayerDisplayName}]"
                    : $"[{requester.PlayerDisplayName}]  {requester.characterName}"; // Intentional double space.
        }

        public void UpdateRowExceptRequester(GMRequestRow row)
        {
            GMRequest request = row.request;

            bool latencyIsRead = request.latencyIsRead;
            row.regularHighlight.SetActive(!latencyIsRead && request.latencyRequestType == GMRequestType.Regular);
            row.urgentHighlight.SetActive(!latencyIsRead && request.latencyRequestType == GMRequestType.Urgent);
            row.readToggle.SetIsOnWithoutNotify(latencyIsRead);

            RPPlayerData responder = request.latencyRespondingPlayer;
            row.responderText.text = responder == null ? "" : $"Responder:  {responder.PlayerDisplayName}"; // Intentional double space.

            UpdateRowTimeInfo(row); // Must do this after updating regular/urgent highlights, as this might toggle those.
        }

        public void UpdateRowTimeInfo(GMRequestRow row)
        {
            GMRequest request = row.request;
            string postfix = request.latencyRequestType == GMRequestType.Urgent ? "  [Urgent]" : ""; // Intentional double space.
            if (request.isLatency)
            {
                row.timeAndInfoText.text = "just now" + postfix;
                return;
            }
            uint liveTicks = lockstep.CurrentTick - request.requestedAtTick;
            int seconds = Mathf.FloorToInt(liveTicks / LockstepAPI.TickRate);
            UpdateRowPresentedAsUrgent(row, seconds);
            int minutes = seconds / 60;
            if (minutes == 0)
            {
                row.timeAndInfoText.text = $"{seconds}s ago{postfix}";
                return;
            }
            seconds -= minutes * 60;
            row.timeAndInfoText.text = $"{minutes}m {seconds}s ago{postfix}";
        }

        private void UpdateRowPresentedAsUrgent(GMRequestRow row, int seconds)
        {
            GMRequest request = row.request;
            if (request.latencyIsRead || request.latencyRequestType != GMRequestType.Regular)
                return;
            int presentAsUrgentAfterSeconds = requestsManager.PresentAsUrgentAfterSeconds;
            if (presentAsUrgentAfterSeconds == -1)
                return;
            bool becameUrgent = seconds >= presentAsUrgentAfterSeconds;
            row.regularHighlight.SetActive(!becameUrgent);
            row.urgentHighlight.SetActive(becameUrgent);
        }

        #endregion

        #region SortAPI

        public void ResortRow(GMRequestRow row) => SortOne(row);

        #endregion

        #region MergeSortComparators

        public void CompareRow()
        {
            GMRequest requestLeft = ((GMRequestRow)compareLeft).request;
            GMRequest requestRight = ((GMRequestRow)compareRight).request;
            bool isRead = requestRight.isRead;
            if (requestLeft.isRead != isRead)
            {
                leftSortsFirst = isRead;
                return;
            }
            // isRead means "are read" from this point forward.
            if (!isRead && requestLeft.requestType != requestRight.requestType)
            {
                leftSortsFirst = requestLeft.requestType == GMRequestType.Urgent;
                return;
            }
            if (requestLeft.isLatency || requestRight.isLatency)
            {
                leftSortsFirst = (requestLeft.isLatency && requestRight.isLatency)
                    // Since both are by the same player this will actually reliably sort old first, although
                    // that is relying on a lockstep implementation detail that is actually undefined behavior.
                    // But even if the order was random - but stable - that would be fine.
                    ? requestLeft.uniqueId < requestRight.uniqueId
                    : requestRight.isLatency; // Latency state is new, so put it at the end.
                leftSortsFirst = leftSortsFirst != isRead; // Invert when requests are read, new requests first.
                return;
            }
            int compared = requestLeft.requestedAtTick.CompareTo(requestRight.requestedAtTick);
            if (compared != 0)
            {
                leftSortsFirst = (compared < 0) != isRead; // Old requests come first, inverted when read.
                return;
            }
            // Again old first, inverted when read. And these are never equal.
            leftSortsFirst = (requestLeft.id < requestRight.id) != isRead;
        }

        #endregion
    }
}
