using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RPMenuTeleportManager : RPMenuTeleportManagerAPI
    {
        public LayerMask localPlayerCollidingLayers;

        // DEBUG
        public Transform debugOtherPlayer;
        public Transform debugLocalPlayer;

        private const float DesiredDistanceFromOtherPlayers = 1.25f;
        private const float MinDistanceFromOtherPlayers = 0.75f;
        private const float MaxRelativeDownwardsDistance = 0.4f;
        private const float SafetyDistanceFromGround = 0.015f;
        private const float SafetyDistanceFromWalls = 0.1f;

        private Quaternion[] directionsToTry = new Quaternion[]
        {
            Quaternion.identity,
            Quaternion.AngleAxis(60f, Vector3.up),
            Quaternion.AngleAxis(-60f, Vector3.up),
            Quaternion.AngleAxis(120f, Vector3.up),
            Quaternion.AngleAxis(-120f, Vector3.up),
            Quaternion.AngleAxis(180f, Vector3.up),
        };
        private int directionsToTryCount;

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            directionsToTryCount = directionsToTry.Length;
        }

        private void Update()
        {
            // DEBUG
            FindTarget(debugOtherPlayer.position, debugOtherPlayer.rotation, Vector3.forward, out Vector3 position, out Quaternion rotation);
            debugLocalPlayer.SetPositionAndRotation(position, rotation);
        }

        public override void TeleportToPlayer(VRCPlayerApi otherPlayer, Vector3 desiredRelativeDirection)
        {
            TeleportToPlayer(otherPlayer.GetPosition(), otherPlayer.GetRotation(), desiredRelativeDirection);
        }

        public override void TeleportToPlayer(Vector3 otherPosition, Quaternion otherRotation, Vector3 desiredRelativeDirection)
        {
            FindTarget(otherPosition, otherRotation, desiredRelativeDirection, out Vector3 position, out Quaternion rotation);
            localPlayer.TeleportTo(position, rotation);
        }

        public void FindTarget(
            Vector3 otherPosition,
            Quaternion otherRotation,
            Vector3 desiredRelativeDirection,
            out Vector3 position,
            out Quaternion rotation)
        {
            float radius = LocalPlayerCapsule.GetRadius();
            float height = LocalPlayerCapsule.GetHeight() - radius;
            Vector3 centerPosition = otherPosition + Vector3.up * height;
            float longestDistance = -1f;
            Vector3 bestDirection = Vector3.zero;
            RaycastHit hit;
            for (int i = 0; i < directionsToTryCount; i++)
            {
                Vector3 direction = otherRotation * directionsToTry[i] * desiredRelativeDirection;
                if (!Physics.SphereCast(
                    centerPosition,
                    radius,
                    direction,
                    out hit,
                    DesiredDistanceFromOtherPlayers + SafetyDistanceFromWalls,
                    localPlayerCollidingLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    longestDistance = DesiredDistanceFromOtherPlayers;
                    bestDirection = direction;
                    break;
                }

                float distance = hit.distance - SafetyDistanceFromWalls;
                if (distance >= MinDistanceFromOtherPlayers)
                {
                    longestDistance = distance;
                    bestDirection = direction;
                    break;
                }

                if (distance > longestDistance)
                {
                    longestDistance = distance;
                    bestDirection = direction;
                }
            }

            position = centerPosition + bestDirection * longestDistance;
            if (Physics.SphereCast(
                position,
                radius,
                Vector3.down,
                out hit,
                height + MaxRelativeDownwardsDistance + SafetyDistanceFromGround,
                localPlayerCollidingLayers,
                QueryTriggerInteraction.Ignore))
            {
                position += Vector3.down * (hit.distance + radius - SafetyDistanceFromGround);
            }
            else
                position.y = otherPosition.y; // Potentially off a cliff, so stay on the same y as the target.

            Vector3 positionOnYPlane = position;
            positionOnYPlane.y = 0f;
            otherPosition.y = 0f;
            rotation = Quaternion.LookRotation(otherPosition - positionOnYPlane);
        }
    }
}
