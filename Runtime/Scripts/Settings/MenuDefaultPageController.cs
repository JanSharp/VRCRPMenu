using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuDefaultPageController : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private MenuSettingsManagerAPI menuSettingsManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        [SerializeField] private string homePageInternalName = "rp-menu.home";
        private MenuPageRoot homePageRoot;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            homePageRoot = menuManager.GetPageRoot(homePageInternalName);
            if (homePageRoot == null)
                Debug.LogError($"[RPMenu] The Home Page Internal Name for the Menu Default Page Controller "
                    + $"is set to '{homePageInternalName}' however no page with such an internal name exists "
                    + $"in the associated menu ({menuManager.name}).", this);
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuOpenStateChanged, Order = 1000)]
        public void OnMenuOpenStateChanged()
        {
            // Must delay as setting the active page raises an event instantly, which would end up overwriting
            // the OnMenuOpenStateChanged listeners. Because Udon is a genuinely terrible language with a
            // broken (effectively non existent) stack system. Sick and tired of it.
            SendCustomEventDelayedFrames(nameof(OnMenuOpenStateChangedDelayed), 1);
        }

        public void OnMenuOpenStateChangedDelayed()
        {
            if (menuManager.IsMenuOpen)
                return; // Cleaner to change the active page upon close, especially with that 1 frame delay.
            RPMenuDefaultPageType defaultPage = menuSettingsManager.LatencyDefaultPage;
            if (defaultPage != RPMenuDefaultPageType.Home || menuManager.OpenPopupsCount != 0 || homePageRoot == null)
                return;
            menuManager.SetActivePage(homePageRoot);
        }
    }
}
