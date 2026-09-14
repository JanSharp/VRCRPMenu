using TMPro;
using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class SortableScrollableSearchableRow : SortableScrollableRow
    {
        public abstract TextMeshProUGUI SearchableNameLabel { get; }
        public abstract string SearchableName { get; }

        /// <summary><para>Cached upon row creation.</para></summary>
        [System.NonSerialized] public string richTextEscapedSearchableName;

        /// <summary><para>Cached upon row creation.</para></summary>
        [System.NonSerialized] public string[] intermediates;
        /// <summary><para>Cached upon row creation.</para></summary>
        [System.NonSerialized] public string[] mixedCasingWords;
        /// <summary><para>Cached upon row creation.</para></summary>
        [System.NonSerialized] public string[] words;
        /// <summary><para>Cached upon row creation.</para></summary>
        [System.NonSerialized] public int totalWordsLetterCount;

        [System.NonSerialized] public int firstMatchingLetterIndex;
        [System.NonSerialized] public int longestConsecutiveMatch;
        [System.NonSerialized] public bool anyMatchesAreBeginningsOfWords;
        [System.NonSerialized] public bool allMatchesAreBeginningsOfWords;
    }
}
