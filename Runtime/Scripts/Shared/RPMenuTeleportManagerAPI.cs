using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [SingletonScript("3b1d00e6074519a39b78235f631308a1")] // Runtime/Prefabs/Managers/RPMenuTeleportManager.prefab
    public abstract class RPMenuTeleportManagerAPI : UdonSharpBehaviour
    {
        public abstract void TeleportToPlayer(VRCPlayerApi otherPlayer, Vector3 desiredRelativeDirection);
        public abstract void TeleportToPlayer(Vector3 otherPosition, Quaternion otherRotation, Vector3 desiredRelativeDirection);
    }
}
