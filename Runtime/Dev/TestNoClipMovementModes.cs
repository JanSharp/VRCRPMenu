using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestNoClipMovementModes : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipMovementAPI noClipMovement;

        public ToggleGroup stillToggleGroup;
        public ToggleGroup movingToggleGroup;
        public Toggle stillVelocityToggle;
        public Toggle stillFakeGroundToggle;
        public Toggle movingVelocityToggle;
        public Toggle movingComboToggle;
        public Toggle movingTeleportToggle;

        private void Start()
        {
            stillToggleGroup.allowSwitchOff = true;
            movingToggleGroup.allowSwitchOff = true;
            stillVelocityToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileStill == NoClipModeWhileStill.Velocity);
            stillFakeGroundToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileStill == NoClipModeWhileStill.FakeGround);
            movingVelocityToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileMoving == NoClipModeWhileMoving.Velocity);
            movingComboToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileMoving == NoClipModeWhileMoving.Combo);
            movingTeleportToggle.SetIsOnWithoutNotify(noClipMovement.ModeWhileMoving == NoClipModeWhileMoving.Teleport);
            stillToggleGroup.allowSwitchOff = false;
            movingToggleGroup.allowSwitchOff = false;
        }

        public void OnStillToggleValueChanged()
        {
            if (stillVelocityToggle.isOn)
                noClipMovement.ModeWhileStill = NoClipModeWhileStill.Velocity;
            else if (stillFakeGroundToggle.isOn)
                noClipMovement.ModeWhileStill = NoClipModeWhileStill.FakeGround;
        }

        public void OnMovingToggleValueChanged()
        {
            if (movingVelocityToggle.isOn)
                noClipMovement.ModeWhileMoving = NoClipModeWhileMoving.Velocity;
            else if (movingComboToggle.isOn)
                noClipMovement.ModeWhileMoving = NoClipModeWhileMoving.Combo;
            else if (movingTeleportToggle.isOn)
                noClipMovement.ModeWhileMoving = NoClipModeWhileMoving.Teleport;
        }
    }
}
