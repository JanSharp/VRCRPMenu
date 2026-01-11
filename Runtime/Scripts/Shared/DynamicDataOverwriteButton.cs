using TMPro;
using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DynamicDataOverwriteButton : UdonSharpBehaviour
    {
        [System.NonSerialized] public DynamicData dynamicData;
        [System.NonSerialized] public string sortableDataName;
        public DynamicDataSavePopup popupScript;
        public Button button;
        public TextMeshProUGUI label;
    }
}
