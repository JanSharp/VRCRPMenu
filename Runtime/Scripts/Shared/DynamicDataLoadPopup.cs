using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    public abstract class DynamicDataLoadPopup : DynamicDataPopupList
    {
        public Button mainButton;
        public Selectable mainButtonLabel;
        public Selectable localLabelStyle;
        public Selectable globalLabelStyle;
        public Transform popupLocation;
        public LayoutElement deleteButtonLayoutElement;
        private float popupWidthWithDeleteButtons;
        private float popupWidthWithoutDeleteButtons;
        public float popupSpacingToEdge = 20f;

        private bool popupIsShown = false;

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

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            ClosePopup(doSendDeleteIAs: false);
        }

        public override void Resolve()
        {
            UpdateDynamicDataPopupWidth();
            base.Resolve();
            if (popupIsShown
                && !localLoadPDef.valueForLocalPlayer
                && !localDeletePDef.valueForLocalPlayer
                && !globalLoadPDef.valueForLocalPlayer
                && !globalDeletePDef.valueForLocalPlayer)
            {
                ClosePopup(doSendDeleteIAs: false);
            }
        }

        private void UpdateDynamicDataPopupWidth()
        {
            Vector2 size = popupRoot.sizeDelta;
            size.x = localDeletePDef.valueForLocalPlayer || globalDeletePDef.valueForLocalPlayer
                ? popupWidthWithDeleteButtons
                : popupWidthWithoutDeleteButtons;
            popupRoot.sizeDelta = size;

            if (popupIsShown)
                menuManager.PushShownPopupOntoPage(popupRoot, popupSpacingToEdge);
        }

        public void OnMainClick()
        {
            if (popupIsShown)
                return;
            popupIsShown = true;
            popupRoot.anchoredPosition = Vector2.zero;
            menuManager.ShowPopupAtCurrentPosition(popupRoot, this, nameof(OnPopupClosed), popupSpacingToEdge);
        }

        public void OnPopupClosed()
        {
            OnPopupClosedInternal();
            ProcessAllMarkedForDeletion(doSendDeleteIAs: true);
        }

        private void ClosePopup(bool doSendDeleteIAs)
        {
            if (!popupIsShown)
                return;
            menuManager.ClosePopup(popupRoot, doCallback: false);
            OnPopupClosedInternal();
            ProcessAllMarkedForDeletion(doSendDeleteIAs);
        }

        private void OnPopupClosedInternal()
        {
            popupIsShown = false;
            popupRoot.SetParent(popupLocation, worldPositionStays: false);
        }

        private void ProcessAllMarkedForDeletion(bool doSendDeleteIAs)
        {
            DynamicDataLoadDeleteButton[] loadButtons = (DynamicDataLoadDeleteButton[])buttons;
            for (int i = 0; i < buttonsCount; i++)
            {
                DynamicDataLoadDeleteButton button = loadButtons[i];
                if (!button.markedForDeletion)
                    continue;
                if (doSendDeleteIAs)
                    dynamicDataManager.SendDeleteIA(button.dynamicData);
                SetMarkForDeletion(button, false);
            }
        }

        private bool HasLoadPermission(DynamicData data)
        {
            return data.isGlobal ? globalLoadPDef.valueForLocalPlayer : localLoadPDef.valueForLocalPlayer;
        }

        public void OnLoadButtonClick(DynamicDataLoadDeleteButton button)
        {
            ClosePopup(doSendDeleteIAs: true);
            DynamicData data = button.dynamicData;
            if (HasLoadPermission(data))
                LoadDynamicData(data);
        }

        protected abstract void LoadDynamicData(DynamicData data);

        public void OnDeleteButtonClick(DynamicDataLoadDeleteButton button)
        {
            SetMarkForDeletion(button, !button.markedForDeletion);
        }

        private void SetMarkForDeletion(DynamicDataLoadDeleteButton button, bool marked)
        {
            button.markedForDeletion = marked;
            button.button.interactable = !marked && HasLoadPermission(button.dynamicData);
            button.labelSelectable.interactable = !marked;
            button.deleteIconSelectable.interactable = !marked;
            button.undeleteIconSelectable.interactable = marked;
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
            if (loadButton.markedForDeletion)
                SetMarkForDeletion(loadButton, false);
            else
                button.button.interactable = HasLoadPermission(button.dynamicData);
        }

        protected override void UpdateDueToChangedButtonCount()
        {
            base.UpdateDueToChangedButtonCount();
            bool interactable = buttonsCount != 0;
            mainButton.interactable = interactable;
            mainButtonLabel.interactable = interactable;
            // Not closing the popup if it is open as it is more informative to the user to see the "no data"
            // label rater than the popup suddenly closing on its own. Also prevents accidental clicks through
            // the UI if the user tries to interact with it right as it closes.
        }
    }
}
