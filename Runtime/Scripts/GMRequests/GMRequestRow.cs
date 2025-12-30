using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMRequestRow : SortableScrollableRow
    {
        [System.NonSerialized] public GMRequest request;
        public GMRequestsPage page;
        public CanvasGroup rowGroup;
        public Image urgentHighlight;
        public Toggle readToggle;
        public TextMeshProUGUI timeAndInfoText;
        public TextMeshProUGUI responderText;
        public TextMeshProUGUI requesterText;

        public void OnRespondClick() => page.OnRespondClick(this);

        public void OnJoinClick() => page.OnJoinClick(this);

        public void OnReadToggleValueChanged() => page.OnReadToggleValueChanged(this);
    }
}
