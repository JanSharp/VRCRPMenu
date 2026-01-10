using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ShowSaveDynamicDataPopups : ShowAdjacentPopups
    {
        [PermissionDefinitionReference(nameof(localAddPDef))]
        public string localAddPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localAddPDef;

        [PermissionDefinitionReference(nameof(localOverwritePDef))]
        public string localOverwritePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localOverwritePDef;

        [PermissionDefinitionReference(nameof(globalAddPDef))]
        public string globalAddPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalAddPDef;

        [PermissionDefinitionReference(nameof(globalOverwritePDef))]
        public string globalOverwritePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalOverwritePDef;

        public void OnClick() => ShowPopups();

        protected override bool DoShowMain() => localOverwritePDef.valueForLocalPlayer || globalOverwritePDef.valueForLocalPlayer;

        protected override bool DoShowSide() => localAddPDef.valueForLocalPlayer || globalAddPDef.valueForLocalPlayer;

        protected override void OnPopupsClosed() { }
    }
}
