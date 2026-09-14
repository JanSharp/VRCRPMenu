using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsRow : SortableScrollableSearchableRow
    {
        [System.NonSerialized] public bool isFavorite;
        [System.NonSerialized] public string sortableObjectName;
        [System.NonSerialized] public string sortableCategory;
        [System.NonSerialized] public EntityPrototype entityPrototype;
        public ObjectsPage page;
        public Toggle favoriteToggle;
        public TextMeshProUGUI objectNameLabel;
        public TextMeshProUGUI categoryLabel;
        public Toggle highlightToggle;
        public GameObject categoryRoot;

        public override TextMeshProUGUI SearchableNameLabel => objectNameLabel;
        public override string SearchableName => entityPrototype.DisplayName;

        public void OnFavoriteValueChanged() => page.OnFavoriteValueChanged(this);
        public void OnHighlightToggleValueChanged() => page.OnHighlightToggleValueChanged(this);
    }
}
