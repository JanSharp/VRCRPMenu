using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ShowAdjacentPopups : PermissionResolver
    {
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        [Tooltip("A disabled object containing the Helper Transform and Main and Side popups, which are "
            + "all enabled.\nAlso defines the location that the middle popup should be centered at.")]
        public Transform popupsParent;
        [Tooltip("An empty objet.")]
        public RectTransform helperTransform;
        public RectTransform mainPopup;
        public RectTransform sidePopup;
        public bool showSidePopupOnLeft;
        public float popupSpacing;
        public float popupVerticalOffset;

        private bool popupsAreShown = false;

        [PermissionDefinitionReference(nameof(showMainPDef), Optional = true)]
        public string showMainPermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition showMainPDef;

        [PermissionDefinitionReference(nameof(showSidePDef), Optional = true)]
        public string showSidePermissionAsset; // A guid.
        [HideInInspector][SerializeField] protected PermissionDefinition showSidePDef;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            mainPopup.pivot = new Vector2(0f, mainPopup.pivot.y);
            sidePopup.pivot = new Vector2(0f, sidePopup.pivot.y);
            helperTransform.SetParent(popupsParent, worldPositionStays: false);
            // Using centered like this even though it makes the math more annoying in order for resizing
            // while the popups are shown to be be less annoying (just work).
            helperTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (!popupsAreShown)
                return;
            if (!UpdatePopupParentingAndPositioning(out var discard)) // Cannot use 'out _'.
            {
                menuManager.ClosePopup(helperTransform, doCallback: true);
                return;
            }
            menuManager.PushShownPopupOntoPage(helperTransform, popupSpacing);
        }

        public void ShowPopups()
        {
            if (!UpdatePopupParentingAndPositioning(out float x))
                return;
            helperTransform.anchoredPosition = new Vector2(x, popupVerticalOffset);
            popupsAreShown = true;
            menuManager.ShowPopupAtCurrentPosition(helperTransform, this, nameof(OnPopupsClosed), minDistanceFromPageEdge: popupSpacing);
        }

        public void OnPopupsClosed()
        {
            popupsAreShown = false;
            helperTransform.SetParent(popupsParent, worldPositionStays: false);
        }

        private bool UpdatePopupParentingAndPositioning(out float helperXPosition)
        {
            bool doShowMain = showMainPDef == null || showMainPDef.valueForLocalPlayer;
            bool doShowSide = showSidePDef == null || showSidePDef.valueForLocalPlayer;

            if (!doShowMain && !doShowSide)
            {
                mainPopup.SetParent(popupsParent, worldPositionStays: false);
                sidePopup.SetParent(popupsParent, worldPositionStays: false);
                helperXPosition = 0f;
                return false;
            }

            float mainWidth = mainPopup.sizeDelta.x;
            float sideWidth = sidePopup.sizeDelta.x;
            float width;
            if (doShowMain && doShowSide)
            {
                mainPopup.SetParent(helperTransform, worldPositionStays: false);
                sidePopup.SetParent(helperTransform, worldPositionStays: false);
                width = mainWidth + popupSpacing + sideWidth;
                float halfWidth = width / 2f;
                if (showSidePopupOnLeft)
                {
                    helperXPosition = halfWidth - (sideWidth + popupSpacing + mainWidth / 2f);
                    mainPopup.anchoredPosition = new Vector2(sideWidth + popupSpacing - halfWidth, 0f);
                    sidePopup.anchoredPosition = new Vector2(-halfWidth, 0f);
                }
                else
                {
                    helperXPosition = halfWidth - mainWidth / 2f;
                    mainPopup.anchoredPosition = new Vector2(-halfWidth, 0f);
                    sidePopup.anchoredPosition = new Vector2(mainWidth + popupSpacing - halfWidth, 0f);
                }
            }
            else if (doShowMain)
            {
                sidePopup.SetParent(popupsParent, worldPositionStays: false);
                mainPopup.SetParent(helperTransform, worldPositionStays: false);
                width = mainWidth;
                mainPopup.anchoredPosition = new Vector2(-width / 2f, 0f);
                helperXPosition = 0f;
            }
            else
            {
                mainPopup.SetParent(popupsParent, worldPositionStays: false);
                sidePopup.SetParent(helperTransform, worldPositionStays: false);
                width = sideWidth;
                sidePopup.anchoredPosition = new Vector2(-width / 2f, 0f);
                helperXPosition = 0f;
            }

            helperTransform.sizeDelta = new Vector2(width, 0f);
            return true;
        }
    }
}
