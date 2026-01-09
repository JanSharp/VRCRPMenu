using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersRow : SortableScrollableRow
    {
        [System.NonSerialized] public bool isFavorite;
        [System.NonSerialized] public string sortablePlayerName;
        [System.NonSerialized] public string sortableCharacterName;
        [System.NonSerialized] public float sortableProximity;
        [System.NonSerialized] public int sortableSelection;
        [System.NonSerialized] public RPPlayerData rpPlayerData;
        public PlayersPage page;
        public Toggle favoriteToggle;
        public TextMeshProUGUI playerNameLabel;
        public TextMeshProUGUI characterNameLabel;
        public TextMeshProUGUI proximityLabel;
        public Toggle selectToggle;
        public GameObject characterNameRoot;
        public GameObject teleportToRoot;
        public GameObject proximityRoot;
        public GameObject selectRoot;

        public void OnFavoriteValueChanged() => page.OnFavoriteValueChanged(this);
        public void OnTeleportToClick() => page.OnTeleportToClick(this);
        public void OnSelectValueChanged() => page.OnSelectValueChanged(this);
    }
}
