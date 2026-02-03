using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMProxySpawnButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private GMProxiesManagerAPI gmProxiesManager;

        public TMP_InputField displayNameField;
        public ToggleGroupWithFloatValues scaleToggles;

        public void OnClick()
        {
            // TODO: In VR, figure out which hand the player used to interact with the menu with and place the item at that hand.
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            localPlayer.GetAvatarEyeHeightAsMeters();
            gmProxiesManager.CreateGMProxy(
                head.position + head.rotation * Vector3.forward * 0.5f,
                head.rotation,
                scaleToggles.GetValue(),
                displayNameField.text);
        }
    }
}
