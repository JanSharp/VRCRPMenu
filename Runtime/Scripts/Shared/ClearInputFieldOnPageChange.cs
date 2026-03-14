using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ClearInputFieldOnPageChange : UdonSharpBehaviour
    {
        public TMP_InputField inputField;
        public bool doClearInVR = true;
        [Tooltip("Due to the setting which makes the menu automatically change to the home page upon closing "
            + "the menu, in desktop it's likely generally best not to clear on page change since typing into "
            + "an input field inherently closes the menu.")]
        public bool doClearInDesktop = false;
        private bool isInVR;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            isInVR = Networking.LocalPlayer.IsUserInVR();
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuActivePageChanged)]
        public void OnMenuActivePageChanged()
        {
            if (isInVR ? doClearInVR : doClearInDesktop)
                inputField.text = "";
        }
    }
}
