using TMPro;
using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeleportLocationRow : SortableScrollableRow
    {
        /// <summary>
        /// <para><see cref="locationOrder"/> and <see cref="sortableOrder"/> have the same value, they are
        /// just semantically different.</para>
        /// <para><see cref="locationOrder"/> is used as an id.</para>
        /// <para><see cref="sortableOrder"/> is used for sorting.</para>
        /// </summary>
        [System.NonSerialized] public int locationOrder;
        /// <inheritdoc cref="locationOrder"/>
        [System.NonSerialized] public int sortableOrder;
        [System.NonSerialized] public string sortableLocationName;
        [System.NonSerialized] public string sortableLocationCategory;
        [System.NonSerialized] public TeleportLocation location;
        public TeleportLocationsPage page;
        public TextMeshProUGUI locationOrderLabel;
        public TextMeshProUGUI locationNameLabel;
        public TextMeshProUGUI locationCategoryLabel;

        public void OnTeleportClick() => page.OnTeleportClick(this);
    }
}
