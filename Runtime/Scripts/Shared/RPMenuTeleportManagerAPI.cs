using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [SingletonScript("3b1d00e6074519a39b78235f631308a1")] // Runtime/Prefabs/Managers/RPMenuTeleportManager.prefab
    public abstract class RPMenuTeleportManagerAPI : UdonSharpBehaviour
    {
        public const float DesiredDistanceFromOtherPlayers = 1.25f;
        public const float MinDistanceFromOtherPlayers = 0.75f;
        public const float MaxRelativeDownwardsDistance = 0.4f;
        public const float SafetyDistanceFromGround = 0.015f;
        public const float SafetyDistanceFromWalls = 0.1f;

        public abstract LayerMask LocalPlayerCollidingLayers { get; }
        public abstract void TeleportToPlayer(VRCPlayerApi otherPlayer, Vector3 desiredRelativeDirection);
        public abstract void TeleportToPlayer(Vector3 otherPosition, Quaternion otherRotation, Vector3 desiredRelativeDirection);
        public abstract void TeleportTo(Vector3 position, Quaternion rotation);
    }
}
