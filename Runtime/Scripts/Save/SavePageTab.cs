using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    public abstract class SavePageTab : UdonSharpBehaviour
    {
        public SavePage page;
        public SavePageTabType tabType;
        public Toggle tabToggle;
        public GameObject tabRoot;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public virtual void OnMenuManagerStart()
        {
            tabToggle.SetIsOnWithoutNotify(false);
            tabRoot.SetActive(false);
        }

        public void OnTabToggleValueChanged()
        {
            if (!tabToggle.isOn)
            {
                tabToggle.SetIsOnWithoutNotify(true);
                return;
            }
            page.OnTabToggleValueChanged(this);
        }

        // These get raised in this order.
        // They are guaranteed to be run only once lockstep IsInitialized is true, and after each tab has
        // initialized.
        public virtual void OnTabBecameActive()
        {
            tabToggle.SetIsOnWithoutNotify(true);
            tabRoot.SetActive(true);
        }
        /// <summary>
        /// <para>Only the active tab will know about whether the page is active, inactive tabs will always
        /// think the page is inactive.</para>
        /// </summary>
        public abstract void OnPageBecameActive();
        public abstract void OnTabGotShown();
        public abstract void OnTabGotHidden();
        /// <inheritdoc cref="OnPageBecameActive"/>
        public abstract void OnPageBecameInactive();
        public virtual void OnTabBecameInactive()
        {
            tabToggle.SetIsOnWithoutNotify(false);
            tabRoot.SetActive(false);
        }
    }
}
