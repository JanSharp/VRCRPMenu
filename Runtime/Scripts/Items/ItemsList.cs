using System.Text.RegularExpressions;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemsList : SortableScrollableList
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private ItemsPageManagerAPI itemsPageManager;

        public Image sortItemNameAscendingImage;
        public Image sortItemNameDescendingImage;
        public Image sortCategoryAscendingImage;
        public Image sortCategoryDescendingImage;
        public TMP_InputField searchInputField;
        private string sortOrderFunctionPreSearch;
        private Image sortOrderImagePreSearch;

        /// <summary>
        /// <para><see cref="uint"/> entityPrototypeId => <see cref="ItemsRow"/> row</para>
        /// </summary>
        private DataDictionary rowsByPrototypeId = new DataDictionary();
        public ItemsRow[] Rows => (ItemsRow[])rows;
        public int RowsCount => rowsCount;
        public ItemsRow[] HiddenRows => (ItemsRow[])hiddenRows;
        public int HiddenRowsCount => hiddenRowsCount;
        public ItemsRow[] UnusedRows => (ItemsRow[])unusedRows;
        public int UnusedRowsCount => unusedRowsCount;

        private RPPlayerData localPlayer;

        private Regex sanitationRegex;
        private Regex wordsRegex;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            sanitationRegex = new Regex(@"[^A-Za-z0-9]", RegexOptions.Compiled);
            // There might be a way to shorten this regex, however there should be no backtracking so I think it's good as is.
            wordsRegex = new Regex(@"(?>(?<word>(?>[A-Z](?>(?![A-Z][a-z])[A-Z])+|[A-Z]?[a-z]+|[A-Z]|[0-9]+)) *)+", RegexOptions.Compiled);
        }

        public override void Initialize()
        {
            base.Initialize();

            currentSortOrderFunction = nameof(CompareRowItemNameAscending);
            currentSortOrderImage = sortItemNameAscendingImage;
            currentSortOrderImage.enabled = true;
            someRowsAreOutOfSortOrder = false;
        }

        private void FetchLocalPlayer()
        {
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.LocalPlayerData);
        }

        #region RowsManagement

        public bool TryGetRow(uint entityPrototypeId, out ItemsRow row)
        {
            if (rowsByPrototypeId.TryGetValue(entityPrototypeId, out DataToken rowToken))
            {
                row = (ItemsRow)rowToken.Reference;
                return true;
            }
            row = null;
            return false;
        }

        public ItemsRow CreateRow(EntityPrototype prototype)
        {
            ItemsRow row = CreateRowForPrototype(prototype);
            rowsByPrototypeId.Add(prototype.Id, row);
            InsertSortNewRow(row);
            return row;
        }

        public void RemoveRow(ItemsRow row)
        {
            rowsByPrototypeId.Remove(row.entityPrototype.Id);
            RemoveRow((SortableScrollableRow)row);
        }

        public void RebuildRows() => RebuildRows(itemsPageManager.ItemPrototypesCount);

        protected override void OnRowCreated(SortableScrollableRow row) { }

        protected override void OnPreRebuildRows()
        {
            rowsByPrototypeId.Clear();
        }

        protected override SortableScrollableRow RebuildRow(int index)
        {
            EntityPrototype prototype = itemsPageManager.GetItemPrototype(index);
            ItemsRow row = CreateRowForPrototype(prototype);
            rowsByPrototypeId.Add(prototype.Id, row);
            return row;
        }

        private ItemsRow CreateRowForPrototype(EntityPrototype prototype)
        {
            ItemsRow row = (ItemsRow)CreateRow();
            row.entityPrototype = prototype;
            FindWords(row);

            if (localPlayer == null)
                FetchLocalPlayer();
            bool isFavorite = localPlayer.favoriteItemIdsLut.ContainsKey(prototype.Id);
            string itemName = prototype.DisplayName;
            string category = "Category"; // TODO

            row.isFavorite = isFavorite;
            row.sortableItemName = itemName.ToLower();
            row.sortableCategory = category.ToLower();

            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            row.itemNameLabel.text = itemName;
            row.categoryLabel.text = category;
            row.spawnToggle.SetIsOnWithoutNotify(false);
            row.itemNameLabelSelectable.interactable = true;
            row.categoryLabelSelectable.interactable = true;
            row.overlayRoot.SetActive(false);

            if (currentSortOrderFunction == nameof(CompareRowSearchResults))
                row.hidden = EvaluateHiddenCallback(row);

            return row;
        }

        public void SetRowHidden(ItemsRow row, bool hidden)
        {
            if (hidden)
                HideRow(row);
            else
                ShowRow(row);
        }

        #endregion

        #region Search

        private void FindWords(ItemsRow row)
        {
            string name = sanitationRegex.Replace(row.entityPrototype.DisplayName, " ");
            Match match = wordsRegex.Match(name);
            if (!match.Success)
            {
                row.words = new string[0];
                row.totalWordsLetterCount = 0;
                return;
            }
            CaptureCollection captures = match.Groups["word"].Captures;
            int count = captures.Count;
            int totalCharCount = 0;
            string[] words = new string[count];
            for (int i = 0; i < count; i++)
            {
                string word = captures[i].Value.ToLower();
                words[i] = word;
                totalCharCount += word.Length;
            }
            row.words = words;
            row.totalWordsLetterCount = totalCharCount;
        }

        private string prevSearchQuery = "";
        private string searchQuery;
        private int searchQueryLength;

        public void OnSearchFieldValueChanged()
        {
            searchQuery = sanitationRegex.Replace(searchInputField.text, "").ToLower();
            searchQueryLength = searchQuery.Length;
            if (currentSortOrderImage != null)
                currentSortOrderImage.enabled = false;
            if (searchQueryLength != 0)
            {
                if (currentSortOrderFunction != nameof(CompareRowSearchResults))
                {
                    sortOrderFunctionPreSearch = currentSortOrderFunction;
                    sortOrderImagePreSearch = currentSortOrderImage;
                    currentSortOrderFunction = nameof(CompareRowSearchResults);
                    currentSortOrderImage = null;
                }
            }
            else if (currentSortOrderFunction == nameof(CompareRowSearchResults))
            {
                currentSortOrderFunction = sortOrderFunctionPreSearch;
                currentSortOrderImage = sortOrderImagePreSearch;
                currentSortOrderImage.enabled = true;
            }
            UpdateAllHiddenStates(onlyUpdateShown: searchQuery.StartsWith(prevSearchQuery));
            prevSearchQuery = searchQuery;
        }

        protected override bool EvaluateHiddenCallback(SortableScrollableRow row)
        {
            if (searchQueryLength == 0)
                return false;

            ItemsRow itemsRow = (ItemsRow)row;
            string[] words = itemsRow.words;
            int wordCount = words.Length;
            int totalWordsCharCount = itemsRow.totalWordsLetterCount;
            if (wordCount == 0 || searchQueryLength > totalWordsCharCount)
                return true;

            string word = null;
            int wordIndex = -1;
            int wordLength = 0;
            int letterIndex = -1;
            int visitedCount = 0;

            bool matches = true;
            int firstMatchingLetterIndex = 0;
            int longestConsecutiveMatch = 0;
            bool anyMatchesAreBeginningsOfWords = false;
            bool allMatchesAreBeginningsOfWords = true;

            int prevMatchingWordIndex = -1;
            int prevMatchingLetterIndex = -1;
            int consecutiveMatch = 0;

            for (int i = 0; i < searchQueryLength; i++)
            {
                char query = searchQuery[i];
                int remainingToMatch = searchQueryLength - i;
                while (true)
                {
                    if ((++letterIndex) == wordLength)
                    {
                        if ((++wordIndex) == wordCount)
                        {
                            matches = false;
                            break;
                        }
                        word = words[wordIndex];
                        wordLength = word.Length;
                        letterIndex = 0;
                    }
                    char letter = word[letterIndex];
                    visitedCount++;

                    if (letter == query)
                    {
                        if (i == 0)
                            firstMatchingLetterIndex = visitedCount - 1;
                        anyMatchesAreBeginningsOfWords |= letterIndex == 0;
                        if (prevMatchingWordIndex == wordIndex
                            ? letterIndex != prevMatchingLetterIndex + 1
                            : letterIndex != 0)
                        {
                            allMatchesAreBeginningsOfWords = false;
                        }
                        prevMatchingWordIndex = wordIndex;
                        prevMatchingLetterIndex = letterIndex;
                        if ((++consecutiveMatch) > longestConsecutiveMatch)
                            longestConsecutiveMatch = consecutiveMatch;
                        break;
                    }
                    consecutiveMatch = 0;

                    if (remainingToMatch > totalWordsCharCount - visitedCount)
                    {
                        matches = false;
                        break;
                    }
                }
                if (!matches)
                    break;
            }

            if (!matches)
                return true;
            itemsRow.firstMatchingLetterIndex = firstMatchingLetterIndex;
            itemsRow.longestConsecutiveMatch = longestConsecutiveMatch;
            itemsRow.anyMatchesAreBeginningsOfWords = anyMatchesAreBeginningsOfWords;
            itemsRow.allMatchesAreBeginningsOfWords = allMatchesAreBeginningsOfWords;
            return false;
        }

        #endregion

        #region SortHeaders

        // NOTE: Cannot just invert the order of the rows when inverting the order of a sorted column.
        // The categories are the most clear example of this. When inverting the sort order there it makes
        // more sense for just the categories to flip order, while items in those categories retain relative
        // order

        public void OnItemNameSortHeaderClick()
        {
            if (currentSortOrderImage != null)
                currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowItemNameAscending))
            {
                currentSortOrderFunction = nameof(CompareRowItemNameDescending);
                currentSortOrderImage = sortItemNameDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowItemNameAscending);
                currentSortOrderImage = sortItemNameAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        public void OnCategorySortHeaderClick()
        {
            if (currentSortOrderImage != null)
                currentSortOrderImage.enabled = false;
            if (!someRowsAreOutOfSortOrder && currentSortOrderFunction == nameof(CompareRowCategoryAscending))
            {
                currentSortOrderFunction = nameof(CompareRowCategoryDescending);
                currentSortOrderImage = sortCategoryDescendingImage;
            }
            else
            {
                currentSortOrderFunction = nameof(CompareRowCategoryAscending);
                currentSortOrderImage = sortCategoryAscendingImage;
            }
            currentSortOrderImage.enabled = true;
            SortAll();
        }

        #endregion

        #region SortAPI

        public void SortOnPermissionChange(bool itemCategoryValue)
        {
            if (!itemCategoryValue
                && (currentSortOrderFunction == nameof(CompareRowCategoryAscending)
                    || currentSortOrderFunction == nameof(CompareRowCategoryDescending)))
            {
                currentSortOrderFunction = nameof(CompareRowItemNameAscending);
                // No need for null check, it's only null while using CompareRowSearchResults.
                currentSortOrderImage.enabled = false;
                currentSortOrderImage = sortItemNameAscendingImage;
                currentSortOrderImage.enabled = true;
                SortAll();
            }
        }

        public void PotentiallySortChangedFavoriteRow(ItemsRow row)
        {
            UpdateSortPositionDueToValueChange(row);
        }

        public void SortAllRows()
        {
            SortAll();
        }

        #endregion

        #region MergeSortComparators

        public void CompareRowItemNameAscending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableItemName
                    .CompareTo(right.sortableItemName) <= 0;
        }
        public void CompareRowItemNameDescending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableItemName
                    .CompareTo(right.sortableItemName) >= 0;
        }

        public void CompareRowCategoryAscending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableCategory
                    .CompareTo(right.sortableCategory) <= 0;
        }
        public void CompareRowCategoryDescending()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            if (left.isFavorite != right.isFavorite)
                leftSortsFirst = left.isFavorite;
            else
                leftSortsFirst = left.sortableCategory
                    .CompareTo(right.sortableCategory) >= 0;
        }

        public void CompareRowSearchResults()
        {
            ItemsRow left = (ItemsRow)compareLeft;
            ItemsRow right = (ItemsRow)compareRight;
            // Ignores favorites. When searching for something it very most likely isn't a favorite.
            if (left.anyMatchesAreBeginningsOfWords != right.anyMatchesAreBeginningsOfWords)
                leftSortsFirst = left.anyMatchesAreBeginningsOfWords;
            else if (left.allMatchesAreBeginningsOfWords != right.allMatchesAreBeginningsOfWords)
                leftSortsFirst = left.allMatchesAreBeginningsOfWords;
            else if (left.longestConsecutiveMatch != right.longestConsecutiveMatch)
                leftSortsFirst = left.longestConsecutiveMatch >= right.longestConsecutiveMatch;
            else
                leftSortsFirst = left.firstMatchingLetterIndex <= right.firstMatchingLetterIndex;
            return;
        }

        #endregion
    }
}
