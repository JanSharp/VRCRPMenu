using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class SortableScrollableSearchableList : SortableScrollableList
    {
        public TMP_InputField searchInputField;
        private string sortOrderFunctionPreSearch;
        private Image sortOrderImagePreSearch;

        [UIStyleColor(nameof(searchMatchHighlightColor))]
        public string searchMatchHighlightColorName;
        public Color searchMatchHighlightColor;

        private Regex sanitationRegex;
        private Regex wordsRegex;
        private StringBuilder stringBuilder = new StringBuilder();
        private string highlightMarkOpenTag;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public virtual void OnMenuManagerStart()
        {
            sanitationRegex = new Regex(@"[^A-Za-z0-9]", RegexOptions.Compiled);
            // There might be a way to shorten this regex, however there should be no backtracking so I think it's good as is.
            wordsRegex = new Regex(@"(?>(?<word>(?>[A-Z](?>(?![A-Z][a-z])[A-Z])+|[A-Z]?[a-z]+|[A-Z]|[0-9]+)) *)+", RegexOptions.Compiled);
            highlightMarkOpenTag = $"<mark=#{StringUtil.GetHexFromColor(searchMatchHighlightColor, includeAlpha: true)}>";
        }

        #region RowsManagement

        protected void UpdateNewlyCreatedRow(SortableScrollableSearchableRow row)
        {
            string escapedName = EscapeRichText(row.SearchableName);
            row.richTextEscapedSearchableName = escapedName;
            row.SearchableNameLabel.text = escapedName;
            FindWords(row);

            if (currentSortOrderFunction == nameof(CompareRowSearchResults))
                row.hidden = EvaluateHiddenCallback(row);
        }

        #endregion

        #region Search

        private string EscapeRichText(string text, bool doNotWrapInNoParse = false)
        {
            ///cSpell:ignore noparse
            while (text.Contains("<noparse>"))
                text = text.Replace("<noparse>", "");
            while (text.Contains("</noparse>"))
                text = text.Replace("</noparse>", "");
            if (!doNotWrapInNoParse && text.Contains('<'))
                text = $"<noparse>{text}</noparse>";
            return text;
        }

        private void FindWords(SortableScrollableSearchableRow row)
        {
            // Sanitizing here too just so that if there are any noparse tags in the name,
            // they don't show up while searching.
            string displayName = EscapeRichText(row.SearchableName, doNotWrapInNoParse: true);
            string name = sanitationRegex.Replace(displayName, " ");
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
            string[] intermediates = new string[count + 1];
            string[] mixedCasingWords = new string[count];
            string[] words = new string[count];
            int lastCharIndex = 0;
            for (int i = 0; i < count; i++)
            {
                Capture capture = captures[i];
                int index = capture.Index;
                int length = capture.Length;
                string word = capture.Value;
                intermediates[i] = EscapeRichText(displayName.Substring(lastCharIndex, index - lastCharIndex));
                mixedCasingWords[i] = word;
                words[i] = word.ToLower();
                totalCharCount += length;
                lastCharIndex = index + length;
            }
            intermediates[count] = EscapeRichText(displayName.Substring(lastCharIndex));
            row.intermediates = intermediates;
            row.mixedCasingWords = mixedCasingWords;
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
            SortableScrollableSearchableRow searchableRow = (SortableScrollableSearchableRow)row;
            if (searchQueryLength == 0)
            {
                searchableRow.SearchableNameLabel.text = searchableRow.richTextEscapedSearchableName;
                return false;
            }
            bool matches = SearchForQuery(searchableRow);
            searchableRow.SearchableNameLabel.text = matches
                ? BuildItemNameWithHighlights(searchableRow)
                : searchableRow.richTextEscapedSearchableName;
            return !matches; // Return value means "hidden".
        }

        private bool SearchForQuery(SortableScrollableSearchableRow row)
        {
            if (searchQueryLength == 0)
                return true;

            string[] words = row.words;
            int wordCount = words.Length;
            int totalWordsCharCount = row.totalWordsLetterCount;
            if (wordCount == 0 || searchQueryLength > totalWordsCharCount)
                return false;

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
                return false;
            row.firstMatchingLetterIndex = firstMatchingLetterIndex;
            row.longestConsecutiveMatch = longestConsecutiveMatch;
            row.anyMatchesAreBeginningsOfWords = anyMatchesAreBeginningsOfWords;
            row.allMatchesAreBeginningsOfWords = allMatchesAreBeginningsOfWords;
            return true;
        }

        /// <summary>
        /// <para>Expects the given <paramref name="row"/> to fully match the current
        /// <see cref="searchQuery"/>.</para>
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private string BuildItemNameWithHighlights(SortableScrollableSearchableRow row)
        {
            string[] intermediates = row.intermediates;
            string[] mixedCasingWords = row.mixedCasingWords;
            string[] words = row.words;
            int wordCount = words.Length;

            string mixedCasingWord = null;
            string word = null;
            int wordIndex = -1;
            int wordLength = 0;
            int letterIndex = -1;

            for (int i = 0; i < searchQueryLength; i++)
            {
                char query = searchQuery[i];
                while (true)
                {
                    if ((++letterIndex) == wordLength)
                    {
                        wordIndex++;
                        stringBuilder.Append(intermediates[wordIndex]);
                        mixedCasingWord = mixedCasingWords[wordIndex];
                        word = words[wordIndex];
                        wordLength = word.Length;
                        letterIndex = 0;
                    }

                    if (word[letterIndex] != query)
                        stringBuilder.Append(mixedCasingWord[letterIndex]);
                    else
                    {
                        stringBuilder.Append(highlightMarkOpenTag);
                        stringBuilder.Append(mixedCasingWord[letterIndex]);
                        stringBuilder.Append("</mark>");
                        break;
                    }
                }
            }

            if ((++letterIndex) < wordLength)
                stringBuilder.Append(mixedCasingWord, letterIndex, wordLength - letterIndex);
            while ((++wordIndex) < wordCount)
            {
                stringBuilder.Append(intermediates[wordIndex]);
                stringBuilder.Append(mixedCasingWords[wordIndex]);
            }
            stringBuilder.Append(intermediates[wordCount]);

            string result = stringBuilder.ToString();
            stringBuilder.Clear();
            return result;
        }

        #endregion

        #region MergeSortComparators

        public void CompareRowSearchResults()
        {
            SortableScrollableSearchableRow left = (SortableScrollableSearchableRow)compareLeft;
            SortableScrollableSearchableRow right = (SortableScrollableSearchableRow)compareRight;
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
