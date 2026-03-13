using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsActiveToggle : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipMovementAPI noClipMovement;

        public Toggle toggle;
        public Toggle linkedToggle;

        public void OnValueChanged()
        {
            bool isOn = toggle.isOn;
            // Very technically not redundant, though the order of events and specific script setup
            // for this to actually be required is convoluted and unrealistic. But still.
            linkedToggle.SetIsOnWithoutNotify(isOn);
            noClipMovement.IsNoClipActive = isOn;
        }

        private void MakeToggleMatchActiveState()
        {
            bool active = noClipMovement.IsNoClipActive;
            toggle.SetIsOnWithoutNotify(active);
            linkedToggle.SetIsOnWithoutNotify(active);
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart() => MakeToggleMatchActiveState();

        [NoClipMovementEvent(NoClipMovementEventType.OnIsNoClipActiveChanged)]
        public void OnIsNoClipActiveChanged() => MakeToggleMatchActiveState();
    }
}
