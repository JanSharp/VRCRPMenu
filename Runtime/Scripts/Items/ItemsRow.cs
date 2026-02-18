using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemsRow : SortableScrollableRow
    {
        [System.NonSerialized] public bool isFavorite;
        [System.NonSerialized] public string sortableItemName;
        [System.NonSerialized] public string sortableCategory;
        [System.NonSerialized] public EntityPrototype entityPrototype;
        public ItemsPage page;
        public Toggle favoriteToggle;
        public TextMeshProUGUI itemNameLabel;
        public Selectable itemNameLabelSelectable;
        public TextMeshProUGUI categoryLabel;
        public Selectable categoryLabelSelectable;
        public Toggle spawnToggle;
        public GameObject categoryRoot;
        public GameObject overlayRoot;

        public void OnFavoriteValueChanged() => page.OnFavoriteValueChanged(this);
        public void OnSpawnToggleValueChanged() => page.OnSpawnToggleValueChanged(this);
        public void OnConfirmSpawnClick() => page.OnConfirmSpawnClick(this);
        public void OnCancelSpawnClick() => page.OnCancelSpawnClick(this);
    }
}
