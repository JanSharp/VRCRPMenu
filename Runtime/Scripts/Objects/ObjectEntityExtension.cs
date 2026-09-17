using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [DisallowMultipleComponent]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(ObjectEntityHighlight))]
    [AssociatedEntityExtensionData(typeof(ObjectEntityExtensionData))]
    public class ObjectEntityExtension : EntityExtension
    {
        [System.NonSerialized] public ObjectEntityExtensionData data;

        public GameObject creationPreviewPrefab;
        private ObjectEntityHighlight highlight;
        private bool highlightIsShown;

        public override void OnInstantiate()
        {
            highlight = GetComponent<ObjectEntityHighlight>();
        }

        public override void DisassociateFromExtensionDataAndReset(EntityExtension defaultExtension)
        {
            HideHighlight();
            data.ext = null;
            data = null;
        }

        public override void AssociateWithExtensionData()
        {
            data = (ObjectEntityExtensionData)extensionData;
            data.ext = this;
            ApplyExtensionData();
        }

        public override void ApplyExtensionData() { }

        public void ShowHighlight()
        {
            if (highlightIsShown)
                return;
            highlightIsShown = true;
            highlight.ShowHighlight();
        }

        public void HideHighlight()
        {
            if (!highlightIsShown)
                return;
            highlightIsShown = false;
            highlight.HideHighlight();
        }
    }
}
