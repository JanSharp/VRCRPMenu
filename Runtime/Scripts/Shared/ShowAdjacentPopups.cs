using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class ShowAdjacentPopups : PermissionResolver
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
        public float popupSpacing = 20f;

        private bool popupsAreShown = false;

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

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            if (popupsAreShown)
                menuManager.ClosePopup(helperTransform, doCallback: true);
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
            helperTransform.anchoredPosition = new Vector2(x, 0f);
            popupsAreShown = true;
            menuManager.ShowPopupAtCurrentPosition(helperTransform, this, nameof(OnPopupsClosedInternal), minDistanceFromPageEdge: popupSpacing);
        }

        public void OnPopupsClosedInternal()
        {
            popupsAreShown = false;
            helperTransform.SetParent(popupsParent, worldPositionStays: false);
            OnPopupsClosed();
        }

        protected abstract void OnPopupsClosed();

        protected abstract bool DoShowMain();

        protected abstract bool DoShowSide();

        private bool UpdatePopupParentingAndPositioning(out float helperXPosition)
        {
            bool doShowMain = DoShowMain();
            bool doShowSide = DoShowSide();

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
