using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class SortableScrollableList : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] protected LockstepAPI lockstep;

        public GameObject rowPrefab;
        public SortableScrollableRow rowPrefabScript;
        public RectTransform rowsContent;
        public RectTransform rowsViewport;
        private float rowHeight;
        private float negativeRowHeight;
        private float currentRowsContentHeight;
        private int prevFirstVisibleRowIndex = 0;
        private int prevFirstInvisibleRowIndex = 0;
        public ScrollRect rowsScrollRect;
        [Min(0f)]
        [Tooltip("Units per second, so kind of like pixels per second.")]
        public float minScrollRectVelocity = 20f;

        protected SortableScrollableRow[] rows = new SortableScrollableRow[ArrList.MinCapacity];
        protected int rowsCount = 0;
        private SortableScrollableRow[] unusedRows = new SortableScrollableRow[ArrList.MinCapacity];
        private int unusedRowsCount = 0;

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

        public virtual void Initialize()
        {
            rowHeight = rowPrefabScript.rowRect.sizeDelta.y;
            negativeRowHeight = -rowHeight;

            InitializeRowsContent();
        }

        private void InitializeRowsContent()
        {
            // Force a position changed event when the scroll view gets shown the first time,
            // because a y of 1 forces it to move the view to 0.
            // The height of the viewport rect is unknown until the game objects get activated for the first
            // time, making calling ShowOnlyRowsVisibleInViewport in OnInit for example useless as the page
            // is not yet shown.
            rowsContent.anchoredPosition = new Vector2(0f, 1f);
        }

        protected abstract bool ListIsVisible { get; }

        #region RowsManagement

        protected abstract void OnRowCreated(SortableScrollableRow row);
        protected abstract void OnPreRebuildRows();
        protected abstract SortableScrollableRow RebuildRow(int index);

        protected void RebuildRows(int newRowsCount)
        {
            // Debug.Log($"[RPMenuDebug] PlayersBackendPage  RebuildRows");

            if (!lockstep.IsContinuationFromPrevFrame)
            {
                OnPreRebuildRows();
                HideAllCurrentlyVisibleRows();
                ArrList.AddRange(ref unusedRows, ref unusedRowsCount, rows, rowsCount);

                rowsCount = newRowsCount;
                ArrList.EnsureCapacity(ref rows, rowsCount);
                currentRowsContentHeight = rowsCount * rowHeight;
                rowsContent.sizeDelta = new Vector2(0f, currentRowsContentHeight);
            }

            suspensionSw.Restart();
            while (suspendedIndexInArray < rowsCount)
            {
                if (LogicIsRunningLong())
                    return;
                SortableScrollableRow row = RebuildRow(suspendedIndexInArray);
                rows[suspendedIndexInArray] = row;
                suspendedIndexInArray++;
            }
            suspendedIndexInArray = 0;

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
            for (int i = index; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
        }

        public void OnRowsScrollRectValueChanged()
        {
            if (Mathf.Abs(rowsScrollRect.velocity.y) < minScrollRectVelocity)
                rowsScrollRect.velocity = Vector2.zero;
            ShowOnlyRowsVisibleInViewport();
        }

        private void HideAllCurrentlyVisibleRows()
        {
            // Debug.Log($"[RPMenuDebug] PlayersBackendPage  HideAllCurrentlyVisibleRows - prevFirstVisibleRowIndex: {prevFirstVisibleRowIndex}, prevFirstInvisibleRowIndex: {prevFirstInvisibleRowIndex}, rowsCount: {rowsCount}");
            for (int i = prevFirstVisibleRowIndex; i < prevFirstInvisibleRowIndex; i++)
                rows[i].rowGo.SetActive(false);
            prevFirstVisibleRowIndex = 0;
            prevFirstInvisibleRowIndex = 0;
        }

        private void ShowOnlyRowsVisibleInViewport()
        {
            // Always recalculate in order to support the size changing at runtime. Even though at the time of
            // writing this with the current page setup it is static, things might change, people might use it
            // differently.
            float viewportHeight = rowsViewport.rect.height;
            int visibleCount = Mathf.CeilToInt(viewportHeight / rowHeight) + 1;
            float position = rowsContent.anchoredPosition.y;
            int firstVisibleIndex = Mathf.FloorToInt(position / rowHeight);
            int firstInvisibleIndex = firstVisibleIndex + visibleCount;

            if (firstVisibleIndex < 0)
                firstVisibleIndex = 0;
            if (firstInvisibleIndex > rowsCount)
                firstInvisibleIndex = rowsCount;

            // Debug.Log($"[RPMenuDebug] PlayersBackendPage  ShowOnlyRowsVisibleInViewport (inner) - firstVisibleIndex: {firstVisibleIndex}, firstInvisibleIndex: {firstInvisibleIndex}, rowsCount: {rowsCount}, prevFirstVisibleRowIndex: {prevFirstVisibleRowIndex}, prevFirstInvisibleRowIndex: {prevFirstInvisibleRowIndex}");

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
                currentRowsContentHeight = rowsCount * rowHeight;
                rowsContent.sizeDelta = new Vector2(0f, currentRowsContentHeight);
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
            currentRowsContentHeight = rowsCount * rowHeight;
            rowsContent.sizeDelta = new Vector2(0f, currentRowsContentHeight);
            for (int i = index; i < rowsCount; i++)
                SetRowIndex(rows[i], i);
        }

        protected void UpdateSortPositionDueToValueChange(SortableScrollableRow row)
        {
            if (!ListIsVisible)
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
            if (!ListIsVisible)
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
            // Debug.Log($"[RPMenuDebug] PlayersBackendPage  SortAll");
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
