using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EntityTransformGizmoBridge : TransformGizmoBridge
    {
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public TransformGizmo transformGizmo;
        private float inputLookVertical;

        private VRCPlayerApi localPlayer;
        private bool isInVR;

        private VRCPlayerApi.TrackingDataType activeHand;
        private HandType activeHandType;
        private Entity trackedEntity;
        private Transform trackedTransform;
        private float startedTrackingAtTime;

        public bool IsTrackingEntity => trackedEntity != null;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            isInVR = localPlayer.IsUserInVR();
        }

        public void CustomUpdate()
        {
            if (trackedEntity.entityData == null)
                StopTracking();
        }

        public void StopTracking()
        {
            transformGizmo.SetTracked(null, null);
            trackedEntity = null;
            trackedTransform = null;
            updateManager.Deregister(this);
        }

        public void SetTrackedEntity(Entity entity, VRCPlayerApi.TrackingDataType activeHand)
        {
            if (entity == null)
            {
                StopTracking();
                return;
            }

            trackedEntity = entity;
            trackedTransform = entity.transform;
            this.activeHand = activeHand;
            activeHandType = activeHand == VRCPlayerApi.TrackingDataType.RightHand ? HandType.RIGHT : HandType.LEFT;
            startedTrackingAtTime = Time.time;

            transformGizmo.SetTracked(trackedTransform, this);
            updateManager.Register(this);
        }

        public override void GetHead(out Vector3 position, out Quaternion rotation)
        {
            VRCPlayerApi.TrackingData head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            position = head.position;
            rotation = head.rotation;
        }

        public override void GetRaycastOrigin(out Vector3 position, out Quaternion rotation)
        {
            if (isInVR)
            {
                VRCPlayerApi.TrackingData hand = localPlayer.GetTrackingData(activeHand);
                position = hand.position;
                rotation = hand.rotation * transformGizmo.handDirectionOffsetForVR;
            }
            else
            {
                VRCPlayerApi.TrackingData head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                position = head.position;
                rotation = head.rotation;
            }
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!isInVR || args.handType != activeHandType || Time.time == startedTrackingAtTime)
                return;
            if (value)
                transformGizmo.Activate();
            else
                transformGizmo.Deactivate();
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            inputLookVertical = value;
        }

        public override bool ActivateThisFrame()
        {
            return !isInVR && Input.GetMouseButtonDown(0) && Time.time != startedTrackingAtTime;
        }

        public override bool DeactivateThisFrame()
        {
            return !isInVR && Input.GetMouseButtonUp(0);
        }

        public override bool DeactivateAndRevertThisFrame()
        {
            return isInVR
                ? inputLookVertical < -0.4f
                : (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape));
        }

        public override bool SnappingThisFrame()
        {
            return isInVR
                ? inputLookVertical > 0.4f
                : (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        }

        public override bool ShowVisualRaycastThisFrame()
        {
            return isInVR;
        }

        public override void OnPositionModified()
        {
            if (trackedEntity.entityData == null)
                return;
            trackedEntity.FlagForPositionChange(flagForDiscontinuity: DeactivateAndRevertThisFrame());
        }

        public override void OnRotationModified()
        {
            if (trackedEntity.entityData == null)
                return;
            trackedEntity.FlagForRotationChange(flagForDiscontinuity: DeactivateAndRevertThisFrame());
        }

        public override void OnScaleModified()
        {
            if (trackedEntity.entityData == null)
                return;
            trackedEntity.FlagForScaleChange(flagForDiscontinuity: DeactivateAndRevertThisFrame());
        }
    }
}
