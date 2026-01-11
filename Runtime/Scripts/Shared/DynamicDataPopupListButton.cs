using TMPro;
using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    public abstract class DynamicDataPopupListButton : UdonSharpBehaviour
    {
        [System.NonSerialized] public DynamicData dynamicData;
        [System.NonSerialized] public string sortableDataName;
        public abstract DynamicDataPopupList PopupScript { get; }
        public Button button;
        public TextMeshProUGUI label;
    }
}
