using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    public abstract class DynamicDataLoadPopup : DynamicDataPopupList
    {
        public Selectable localLabelStyle;
        public Selectable globalLabelStyle;
        public Transform popupLocation;
        public LayoutElement deleteButtonLayoutElement;
        private float popupWidthWithDeleteButtons;
        private float popupWidthWithoutDeleteButtons;
        public float popupSpacingToEdge = 20f;

        [PermissionDefinitionReference(nameof(localLoadPDef))]
        public string localLoadPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localLoadPDef;

        [PermissionDefinitionReference(nameof(localDeletePDef))]
        public string localDeletePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition localDeletePDef;

        [PermissionDefinitionReference(nameof(globalLoadPDef))]
        public string globalLoadPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalLoadPDef;

        [PermissionDefinitionReference(nameof(globalDeletePDef))]
        public string globalDeletePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition globalDeletePDef;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public override void OnMenuManagerStart()
        {
            base.OnMenuManagerStart();
            popupWidthWithDeleteButtons = popupRoot.sizeDelta.x;
            popupWidthWithoutDeleteButtons = popupWidthWithDeleteButtons - deleteButtonLayoutElement.preferredWidth;
        }

        public override void Resolve()
        {
            UpdateDynamicDataPopupWidth();
            base.Resolve();
        }

        private void UpdateDynamicDataPopupWidth()
        {
            Vector2 size = popupRoot.sizeDelta;
            size.x = localDeletePDef.valueForLocalPlayer || globalDeletePDef.valueForLocalPlayer
                ? popupWidthWithDeleteButtons
                : popupWidthWithoutDeleteButtons;
            popupRoot.sizeDelta = size;

            if (popupRoot.parent != popupLocation)
                menuManager.PushShownPopupOntoPage(popupRoot, popupSpacingToEdge);
        }

        public void OnMainClick()
        {
            popupRoot.anchoredPosition = Vector2.zero;
            menuManager.ShowPopupAtCurrentPosition(popupRoot, this, nameof(OnPopupClosed), popupSpacingToEdge);
        }

        public void OnPopupClosed()
        {
            popupRoot.SetParent(popupLocation, worldPositionStays: false);
            // TODO: Send delete input actions.
        }

        public void OnLoadButtonClick(DynamicDataLoadDeleteButton button)
        {
            menuManager.ClosePopup(popupRoot, doCallback: true);
            // TODO: Check permissions.
            // TODO: Load the selection.
        }

        public void OnDeleteButtonClick(DynamicDataLoadDeleteButton button)
        {
            // TODO: Toggle being marked for deletion.
        }

        protected override bool ShouldShowLocalButtons() => localLoadPDef.valueForLocalPlayer || localDeletePDef.valueForLocalPlayer;

        protected override bool ShouldShowGlobalButtons() => globalLoadPDef.valueForLocalPlayer || globalDeletePDef.valueForLocalPlayer;

        protected override void OnDynamicDataButtonCreated(DynamicDataPopupListButton button, DynamicData data)
        {
            DynamicDataLoadDeleteButton loadButton = (DynamicDataLoadDeleteButton)button;
            bool isGlobal = data.isGlobal;
            loadButton.labelSelectable.colors = isGlobal ? globalLabelStyle.colors : localLabelStyle.colors;
            loadButton.deleteButtonRootGo.SetActive(isGlobal
                ? globalDeletePDef.valueForLocalPlayer
                : localDeletePDef.valueForLocalPlayer);
            // TODO: Ensure it is not marked for deletion.
        }
    }
}
