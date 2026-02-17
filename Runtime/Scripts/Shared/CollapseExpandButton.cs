using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CollapseExpandButton : UdonSharpBehaviour
    {
        public Image collapseImage;
        public Image expandImage;
        public GameObject objToShowWhenExpanded;
        public bool isExpanded;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            UpdateVisualization();
        }

        public void OnClick()
        {
            isExpanded = !isExpanded;
            UpdateVisualization();
        }

        private void UpdateVisualization()
        {
            collapseImage.enabled = isExpanded;
            expandImage.enabled = !isExpanded;
            objToShowWhenExpanded.SetActive(isExpanded);
        }
    }
}
