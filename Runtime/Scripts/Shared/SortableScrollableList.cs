using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class SortableScrollableList : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] protected LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] protected UpdateManager updateManager;
        [HideInInspector][SerializeField][FindInParent] protected MenuManagerAPI menuManager;
        [HideInInspector][SerializeField][FindInParent] protected MenuPageRoot menuPageRoot;

        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public GameObject rowPrefab;
        public SortableScrollableRow rowPrefabScript;
        public RectTransform rowsContent;
        public RectTransform rowsViewport;
        private float rowHeight;
        private float negativeRowHeight;
        private float currentRowsContentHeight;
        /// <summary>
        /// <para>Even if the initial position is actually zero, which it likely is, the visible rows are
        /// still going to update appropriately because the height is going to trigger an update.</para>
        /// </summary>
        private float prevRowsContentPosition = 0f;
        private float prevRowsViewportHeight = 0f;
        private int currentVisibleRowsCount = 0;
        private int prevFirstVisibleRowIndex = 0;
        private int prevFirstInvisibleRowIndex = 0;
        public ScrollRect rowsScrollRect;
        [Min(0f)]
        [Tooltip("Units per second, so kind of like pixels per second.")]
        public float minScrollRectVelocity = 20f;
        public GameObject emptyListInfo;

        protected SortableScrollableRow[] rows = new SortableScrollableRow[ArrList.MinCapacity];
        protected int rowsCount = 0;
        protected SortableScrollableRow[] unusedRows = new SortableScrollableRow[ArrList.MinCapacity];
        protected int unusedRowsCount = 0;

        protected string currentSortOrderFunction;
        /// <summary>
        /// <para>Allowed to be <see langword="null"/> to support lists which don't have clickable sort
        /// headers, yet are still sorted.</para>
        /// </summary>
        protected Image currentSortOrderImage;
        protected bool someRowsAreOutOfSortOrder;

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

        protected SortableScrollableRow compareLeft;
        protected SortableScrollableRow compareRight;
        protected bool leftSortsFirst;

        protected bool pageIsVisible;

        public virtual void Initialize()
        {
            rowHeight = rowPrefabScript.rowRect.sizeDelta.y;
            negativeRowHeight = -rowHeight;
            StartStopUpdateLoop();
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuActivePageChanged)]
        public void OnMenuActivePageChanged() => StartStopUpdateLoop();

        [MenuManagerEvent(MenuManagerEventType.OnMenuOpenStateChanged)]
        public void OnMenuOpenStateChanged() => StartStopUpdateLoop();

        private void StartStopUpdateLoop()
        {
            pageIsVisible = menuManager.IsMenuOpen && menuManager.ActivePage == menuPageRoot;
            if (pageIsVisible)
                OnPageWentVisible();
            else
                OnPageWentInvisible();
        }

        protected virtual void OnPageWentVisible()
        {
            updateManager.Register(this);
            CustomUpdate();
        }

        protected virtual void OnPageWentInvisible()
        {
            updateManager.Deregister(this);
        }

        protected virtual void OnPageUpdateWhileVisible()
        {
            float rowsViewportHeight = rowsViewport.rect.height;
            if (rowsViewportHeight == prevRowsViewportHeight)
                return;
            prevRowsViewportHeight = rowsViewportHeight;
            currentVisibleRowsCount = Mathf.CeilToInt(rowsViewportHeight / rowHeight) + 1;
            ShowOnlyRowsVisibleInViewport();
        }

        public void CustomUpdate() => OnPageUpdateWhileVisible();

        #region RowsManagement

        protected abstract void OnRowCreated(SortableScrollableRow row);
        protected abstract void OnPreRebuildRows();
        protected abstract SortableScrollableRow RebuildRow(int index);

        protected void RebuildRows(int newRowsCount)
        {
            if (!lockstep.IsContinuationFromPrevFrame)
            {
                OnPreRebuildRows();
                HideAllCurrentlyVisibleRows();
                ArrList.AddRange(ref unusedRows, ref unusedRowsCount, rows, rowsCount);
                ArrList.EnsureCapacity(ref rows, newRowsCount);
                rowsCount = 0; // Prevent ShowOnlyRowsVisibleInViewport from messing with stuff since this is spread out across frames.
            }

            suspensionSw.Restart();
            while (suspendedIndexInArray < newRowsCount)
            {
                if (LogicIsRunningLong())
                    return;
                rows[suspendedIndexInArray] = RebuildRow(suspendedIndexInArray);
                suspendedIndexInArray++;
            }
            suspendedIndexInArray = 0;

            rowsCount = newRowsCount;
            OnRowsCountChanged(); // rows must be populated by this point, as this could call ShowOnlyRowsVisibleInViewport.
            SortAll();
        }

        protected SortableScrollableRow CreateRow()
        {
            if (unusedRowsCount != 0)
                return ArrList.RemoveAt(ref unusedRows, ref unusedRowsCount, unusedRowsCount - 1);
            GameObject go = Instantiate(rowPrefab);
            go.transform.SetParent(rowsContent, worldPositionStays: false);
            SortableScrollableRow row = go.GetComponent<SortableScrollableRow>();
            OnRowCreated(row);
            return row;
        }

        protected void RemoveRow(SortableScrollableRow row)
        {
            row.rowGo.SetActive(false);
            ArrList.Add(ref unusedRows, ref unusedRowsCount, row);
            int index = row.index;
            ArrList.RemoveAt(ref rows, ref rowsCount, index);
            OnRowsCountChanged();
            for (int i = index; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
        }

        private void OnRowsCountChanged()
        {
            currentRowsContentHeight = rowsCount * rowHeight;
            if (emptyListInfo != null) // Content height should remain zero anyway, otherwise the alternating background starts drawing.
                emptyListInfo.SetActive(rowsCount == 0);
            rowsContent.sizeDelta = new Vector2(0f, currentRowsContentHeight);
            if (prevFirstVisibleRowIndex >= rowsCount) // Prevent from going out of bounds, causing updates to oob rows.
                prevFirstVisibleRowIndex = System.Math.Max(0, rowsCount - 1);
            if (prevFirstInvisibleRowIndex >= rowsCount) // Prevent from going out of bounds, causing updates to oob rows.
                prevFirstInvisibleRowIndex = System.Math.Max(0, rowsCount - 1);
            if (pageIsVisible)
                ShowOnlyRowsVisibleInViewport();
            else
                prevRowsViewportHeight = 0f; // Force update the next time the page becomes visible.
        }

        public void OnRowsScrollRectValueChanged()
        {
            if (Mathf.Abs(rowsScrollRect.velocity.y) < minScrollRectVelocity)
                rowsScrollRect.velocity = Vector2.zero;
            float position = rowsContent.anchoredPosition.y;
            if (position == prevRowsContentPosition)
                return;
            prevRowsContentPosition = position;
            ShowOnlyRowsVisibleInViewport();
        }

        private void HideAllCurrentlyVisibleRows()
        {
            for (int i = prevFirstVisibleRowIndex; i < prevFirstInvisibleRowIndex; i++)
                rows[i].rowGo.SetActive(false);
            prevFirstVisibleRowIndex = 0;
            prevFirstInvisibleRowIndex = 0;
        }

        private void ShowOnlyRowsVisibleInViewport()
        {
            int firstVisibleIndex = Mathf.FloorToInt(prevRowsContentPosition / rowHeight);
            int firstInvisibleIndex = firstVisibleIndex + currentVisibleRowsCount;

            if (firstVisibleIndex < 0)
                firstVisibleIndex = 0;
            if (firstInvisibleIndex > rowsCount)
                firstInvisibleIndex = rowsCount;

            if (firstInvisibleIndex <= prevFirstVisibleRowIndex // New range is entirely below, no overlap.
                    || firstVisibleIndex >= prevFirstInvisibleRowIndex) // New range is entirely above, no overlap.
            {
                for (int i = prevFirstVisibleRowIndex; i < prevFirstInvisibleRowIndex; i++)
                    rows[i].rowGo.SetActive(false);
                for (int i = firstVisibleIndex; i < firstInvisibleIndex; i++)
                    rows[i].rowGo.SetActive(true);
                prevFirstVisibleRowIndex = firstVisibleIndex;
                prevFirstInvisibleRowIndex = firstInvisibleIndex;
                return;
            }
            // There is overlap.
            // The logic below handles prev and new ranges being of different sizes (visible counts).

            if (prevFirstVisibleRowIndex < firstVisibleIndex)
                for (int i = prevFirstVisibleRowIndex; i < firstVisibleIndex; i++)
                    rows[i].rowGo.SetActive(false);
            else
                for (int i = firstVisibleIndex; i < prevFirstVisibleRowIndex; i++)
                    rows[i].rowGo.SetActive(true);

            if (prevFirstInvisibleRowIndex < firstInvisibleIndex)
                for (int i = prevFirstInvisibleRowIndex; i < firstInvisibleIndex; i++)
                    rows[i].rowGo.SetActive(true);
            else
                for (int i = firstInvisibleIndex; i < prevFirstInvisibleRowIndex; i++)
                    rows[i].rowGo.SetActive(false);

            prevFirstVisibleRowIndex = firstVisibleIndex;
            prevFirstInvisibleRowIndex = firstInvisibleIndex;
        }

        private void SetRowIndex(SortableScrollableRow row, int index)
        {
            row.index = index;
            row.rowRect.anchoredPosition = new Vector2(0f, index * negativeRowHeight);
            row.rowGo.SetActive(prevFirstVisibleRowIndex <= index && index < prevFirstInvisibleRowIndex);
        }

        #endregion

        #region SortAPI

        /// <summary>
        /// <para>Adds <paramref name="row"/> to <see cref="rows"/>.</para>
        /// </summary>
        /// <param name="row"></param>
        protected void InsertSortNewRow(SortableScrollableRow row)
        {
            if (rowsCount == 0)
            {
                ArrList.Add(ref rows, ref rowsCount, row);
                OnRowsCountChanged();
                SetRowIndex(row, 0);
                return;
            }
            compareRight = row;
            int index = rowsCount; // Not -1 because the new row is not in the list yet.
            do
            {
                compareLeft = rows[index - 1];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                index--;
            }
            while (index > 0);
            ArrList.Insert(ref rows, ref rowsCount, row, index);
            OnRowsCountChanged();
            for (int i = index; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
        }

        protected void UpdateSortPositionDueToValueChange(SortableScrollableRow row)
        {
            if (!pageIsVisible)
            {
                SortOne(row);
                return;
            }
            if (someRowsAreOutOfSortOrder || IsInSortedPosition(row))
                return;
            MarkAsSomeRowsNoLongerBeingInSortOrder();
        }

        protected void UpdateSortPositionsDueToMultipleValueChanges()
        {
            if (!pageIsVisible)
                SortAll();
            else
                MarkAsSomeRowsNoLongerBeingInSortOrder();
        }

        private void MarkAsSomeRowsNoLongerBeingInSortOrder()
        {
            if (currentSortOrderImage != null)
                currentSortOrderImage.enabled = false;
            someRowsAreOutOfSortOrder = true;
        }

        private bool IsInSortedPosition(SortableScrollableRow row)
        {
            int index = row.index;

            if (index > 0) // Check if it would move left.
            {
                compareLeft = rows[index - 1];
                compareRight = row;
                SendCustomEvent(currentSortOrderFunction);
                if (!leftSortsFirst)
                    return false;
            }

            if (index < rowsCount - 1) // Check if it would move right.
            {
                compareLeft = row;
                compareRight = rows[index + 1];
                SendCustomEvent(currentSortOrderFunction);
                if (!leftSortsFirst)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// <para><paramref name="row"/> must already be in <see cref="rows"/>.</para>
        /// </summary>
        /// <param name="row"></param>
        protected void SortOne(SortableScrollableRow row)
        {
            int index = row.index;
            int initialIndex = index;

            compareRight = row;
            while (index > 0) // Try move left.
            {
                compareLeft = rows[index - 1];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                rows[index] = compareLeft;
                SetRowIndex(compareLeft, index);
                index--;
            }
            if (index != initialIndex)
            {
                rows[index] = row;
                SetRowIndex(row, index);
                return;
            }

            compareLeft = row;
            while (index < rowsCount - 1) // Try move right.
            {
                compareRight = rows[index + 1];
                SendCustomEvent(currentSortOrderFunction);
                if (leftSortsFirst)
                    break;
                rows[index] = compareRight;
                SetRowIndex(compareRight, index);
                index++;
            }
            if (index != initialIndex)
            {
                rows[index] = row;
                SetRowIndex(row, index);
                return;
            }
        }

        protected void SortAll()
        {
            HideAllCurrentlyVisibleRows();
            MergeSort(currentSortOrderFunction);
            for (int i = 0; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
            someRowsAreOutOfSortOrder = false;
            ShowOnlyRowsVisibleInViewport();
        }

        #endregion

        #region MergeSort

        /// <summary>
        /// <para>Merge sort is a stable sorting algorithm.</para>
        /// </summary>
        /// <param name="sortFunctionName"></param>
        private void MergeSort(string sortFunctionName)
        {
            if (rowsCount <= 1)
                return;
            ArrList.EnsureCapacity(ref mergeSortRowsCopy, rowsCount);
            mergeSortStack[0] = 0;
            mergeSortStack[1] = rowsCount;
            mergeSortStackTop = 1;
            mergeSortSortFunction = sortFunctionName;
            MergeSortRecursive();
        }

        private SortableScrollableRow[] mergeSortRowsCopy = new SortableScrollableRow[ArrList.MinCapacity];
        private int[] mergeSortStack = new int[32];
        private int mergeSortStackTop = -1;
        private string mergeSortSortFunction;

        /// <summary>
        /// <para>Not using <see cref="RecursiveMethodAttribute"/>, manually "optimized".</para>
        /// </summary>
        private void MergeSortRecursive()
        {
            int count = mergeSortStack[mergeSortStackTop];
            if (count <= 1)
            {
                mergeSortStackTop -= 2; // Pop args for this MergeSortRecursive call.
                return;
            }
            int index = mergeSortStack[mergeSortStackTop - 1];
            int leftCount = count / 2;

            ArrList.EnsureCapacity(ref mergeSortStack, mergeSortStackTop + 5); // 5 instead of 4, because top == count - 1.
            mergeSortStack[++mergeSortStackTop] = index + leftCount; // Push args for the second MergeSortRecursive call.
            mergeSortStack[++mergeSortStackTop] = count - leftCount;

            if (leftCount > 1) // Duplicated early check for optimization.
            {
                mergeSortStack[++mergeSortStackTop] = index;
                mergeSortStack[++mergeSortStackTop] = leftCount;
                MergeSortRecursive();
            }

            MergeSortRecursive();

            Merge(mergeSortStack[mergeSortStackTop - 1], mergeSortStack[mergeSortStackTop]);
            mergeSortStackTop -= 2; // Pop args for this MergeSortRecursive call.
        }

        private void Merge(int startIndex, int count)
        {
            int leftCount = count / 2;
            int rightCount = count - leftCount;
            System.Array.Copy(rows, startIndex, mergeSortRowsCopy, 0, leftCount + rightCount);

            int leftIndex = 0;
            int rightIndex = 0;
            int targetIndex = startIndex;
            // Compare until reaching the end of either left or right.
            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                compareLeft = mergeSortRowsCopy[leftIndex];
                compareRight = mergeSortRowsCopy[leftCount + rightIndex];
                SendCustomEvent(mergeSortSortFunction);
                if (leftSortsFirst)
                    rows[targetIndex++] = mergeSortRowsCopy[leftIndex++];
                else
                    rows[targetIndex++] = mergeSortRowsCopy[leftCount + rightIndex++];
            }

            if (leftIndex < leftCount) // Copy remaining left.
                System.Array.Copy(mergeSortRowsCopy, leftIndex, rows, targetIndex, leftCount - leftIndex);
            else // Otherwise copy remaining right (guaranteed to be at least 1 remaining in right).
                System.Array.Copy(mergeSortRowsCopy, leftCount + rightIndex, rows, targetIndex, rightCount - rightIndex);
        }

        #endregion
    }
}
