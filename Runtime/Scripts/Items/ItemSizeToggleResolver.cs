using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemSizeToggleResolver : PermissionResolver
    {
        public Toggle thisToggle;
        public Toggle toChangeToOnPermissionLoss;

        [PermissionDefinitionReference(nameof(viewPDef))]
        public string viewPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewPDef;

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (!viewPDef.valueForLocalPlayer && thisToggle.isOn)
            {
                toChangeToOnPermissionLoss.isOn = true;
                thisToggle.SetIsOnWithoutNotify(false); // In case the entire hierarchy is currently disabled.
            }
            gameObject.SetActive(viewPDef.valueForLocalPlayer);
        }
    }
}
