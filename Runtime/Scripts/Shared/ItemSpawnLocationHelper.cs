using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ItemSpawnLocationHelper : ItemSpawnLocationHelperAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private InputManagerAPI inputManager;
        [HideInInspector][SerializeField][SingletonReference] private MenuInputHandler menuInputHandler;
        [HideInInspector][SerializeField][SingletonReference] private CustomInteractablesManagerAPI interactables;

        private VRCPlayerApi localPlayer;
        private bool isInVR;

        private const float DistanceFromHeadInDesktop = 0.6f;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            isInVR = localPlayer.IsUserInVR();
        }

        public override void DetermineItemSpawnLocation(UdonSharpBehaviour callbackInst, string callbackEventName, object callbackCustomData)
        {
            if (!isInVR)
            {
                // Tested it, ignore the eye height, use a constant distance.
                var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                determinedRotation = head.rotation;
                determinedPosition = head.position + determinedRotation * Vector3.forward * DistanceFromHeadInDesktop;
                this.callbackCustomData = callbackCustomData;
                callbackInst.SendCustomEvent(callbackEventName);
                return;
            }

            MenuPositionType menuPosition = menuInputHandler.MenuPosition;
            // Explicitly checking for these 2 types in case more menu position types get added in the future.
            if (menuPosition != MenuPositionType.LeftHand && menuPosition != MenuPositionType.RightHand)
            {
                inputManager.DetermineHandUsedForClick(this, nameof(OnHandDetermined), new object[]
                {
                    callbackInst,
                    callbackEventName,
                    callbackCustomData,
                });
                return;
            }

            var trackingType = menuPosition == MenuPositionType.LeftHand
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;
            var hand = localPlayer.GetTrackingData(trackingType);
            determinedPosition = hand.position;
            determinedRotation = hand.rotation * interactables.GetHandRotationNormalization(trackingType);
            this.callbackCustomData = callbackCustomData;
            callbackInst.SendCustomEvent(callbackEventName);
        }

        public void OnHandDetermined()
        {
            var trackingType = inputManager.DeterminedHand;
            var hand = localPlayer.GetTrackingData(trackingType);
            determinedPosition = hand.position;
            determinedRotation = hand.rotation * interactables.GetHandRotationNormalization(trackingType);
            object[] callbackData = (object[])inputManager.CallbackCustomData;
            callbackCustomData = callbackData[2];
            ((UdonSharpBehaviour)callbackData[0]).SendCustomEvent((string)callbackData[1]);
        }

        private Vector3 determinedPosition;
        private Quaternion determinedRotation;
        private object callbackCustomData;
        public override Vector3 DeterminedPosition => determinedPosition;
        public override Quaternion DeterminedRotation => determinedRotation;
        public override object CallbackCustomData => callbackCustomData;
    }
}
