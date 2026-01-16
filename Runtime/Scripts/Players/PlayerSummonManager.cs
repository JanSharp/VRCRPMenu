using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSummonManager : PlayerSummonManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private RPMenuTeleportManagerAPI teleportManager;
        [HideInInspector][SerializeField][SingletonReference] private WannaBeClassesManager wannaBeClasses;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private const float MinSummonRadius = 2f;
        private const float MaxSummonRadius = 7f;
        private const float MaxRelativeDownwardsDistancePerSummonRadius = 0.5f;
        private const float DesiredDistanceBetweenSummonPositions = 0.4f;
        private const float AnglePerPlayerWithMinSummonRadius = 360f * (DesiredDistanceBetweenSummonPositions / (MinSummonRadius * Mathf.PI));

        private const float ExtraSummonDelayToCoverLatency = 0.6f;

        [SerializeField] private GameObject summonIndicatorPrefab;
        [SerializeField] private Transform summonIndicatorsContainer;
        [Min(0f)]
        [SerializeField] private float summonDelay = 3f;

        [PermissionDefinitionReference(nameof(summonPlayersPDef))]
        public string summonPlayersPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition summonPlayersPDef;

        private PlayerSummonIndicatorGroup[] inactiveIndicatorGroups = new PlayerSummonIndicatorGroup[ArrList.MinCapacity];
        private int inactiveIndicatorGroupsCount = 0;
        private GameObject[] inactiveIndicators = new GameObject[ArrList.MinCapacity];
        private int inactiveIndicatorsCount = 0;

        // // DEBUG
        // public Transform debugLocalPlayer;
        // public Transform[] debugPlayersToSummon;
        // private Vector3[] debugPlayersToSummonPositions;

        private LayerMask localPlayerCollidingLayers;

        private void Start()
        {
            localPlayerCollidingLayers = teleportManager.LocalPlayerCollidingLayers;
            // // DEBUG
            // debugPlayersToSummonPositions = new Vector3[debugPlayersToSummon.Length];
        }

        private void Update()
        {
            // // DEBUG
            // Vector3 position = localPlayer.GetPosition();
            // FindSummonPositions(position, localPlayer.GetRotation(), debugPlayersToSummonPositions);
            // Vector3 centerPosition = position;
            // centerPosition.y = 0f;
            // for (int i = 0; i < debugPlayersToSummon.Length; i++)
            // {
            //     Transform player = debugPlayersToSummon[i];
            //     position = debugPlayersToSummonPositions[i];
            //     player.position = position;
            //     position.y = 0f;
            //     player.rotation = Quaternion.LookRotation(position - centerPosition);
            // }
        }

        private void FindSummonPositions(Vector3 center, Quaternion rotation, GameObject[] targetPositions)
        {
            int count = targetPositions.Length;
            float radius = count * DesiredDistanceBetweenSummonPositions / Mathf.PI;
            Quaternion rotationPerPlayer;
            if (radius < MinSummonRadius)
            {
                radius = MinSummonRadius;
                rotationPerPlayer = Quaternion.AngleAxis(AnglePerPlayerWithMinSummonRadius, Vector3.up);
                // count - 1 because we want the amount of gaps between players, excluding the one larger gap.
                rotation *= Quaternion.AngleAxis(AnglePerPlayerWithMinSummonRadius * (count - 1) / -2f, Vector3.up);
            }
            else
            {
                radius = Mathf.Min(radius, MaxSummonRadius);
                rotationPerPlayer = Quaternion.AngleAxis(360f / count, Vector3.up);
            }

            float capsuleRadius = LocalPlayerCapsule.GetRadius();
            float height = LocalPlayerCapsule.GetHeight() - capsuleRadius;
            Vector3 elevatedCenter = center + Vector3.up * height;
            float downwardsCastDistance = height + radius * MaxRelativeDownwardsDistancePerSummonRadius + RPMenuTeleportManagerAPI.SafetyDistanceFromGround;

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = rotation * Vector3.forward;
                Vector3 position;
                if (Physics.SphereCast(
                    elevatedCenter,
                    capsuleRadius,
                    direction,
                    out RaycastHit hit,
                    radius + RPMenuTeleportManagerAPI.SafetyDistanceFromWalls,
                    localPlayerCollidingLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    position = elevatedCenter + direction * (hit.distance - RPMenuTeleportManagerAPI.SafetyDistanceFromWalls);
                }
                else
                    position = elevatedCenter + direction * radius;

                if (Physics.SphereCast(
                    position,
                    capsuleRadius,
                    Vector3.down,
                    out hit,
                    downwardsCastDistance,
                    localPlayerCollidingLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    position += Vector3.down * (hit.distance + capsuleRadius - RPMenuTeleportManagerAPI.SafetyDistanceFromGround);
                }
                else
                    position.y = center.y; // Potentially off a cliff, so stay on the same y as the target.

                targetPositions[i].transform.position = position;
                rotation *= rotationPerPlayer;
            }
        }

        public override PlayerSummonIndicatorGroup ShowIndicatorsInACircle(Vector3 position, Quaternion rotation, int indictorCount)
        {
            PlayerSummonIndicatorGroup indicatorGroup = GetIndicatorGroup();
            GameObject[] indicators = GetIndicators(indictorCount);
            FindSummonPositions(position, rotation, indicators);
            indicatorGroup.Show(position, rotation, indicators);
            return indicatorGroup;
        }

        private PlayerSummonIndicatorGroup GetIndicatorGroup()
        {
            return inactiveIndicatorGroupsCount != 0
                ? ArrList.RemoveAt(ref inactiveIndicatorGroups, ref inactiveIndicatorGroupsCount, inactiveIndicatorGroupsCount - 1)
                : wannaBeClasses.New<PlayerSummonIndicatorGroup>(nameof(PlayerSummonIndicatorGroup));
        }

        private GameObject[] GetIndicators(int count)
        {
            GameObject[] indicators = new GameObject[count];
            int reusedCount;
            if (inactiveIndicatorsCount == 0)
                reusedCount = 0;
            else
            {
                reusedCount = Mathf.Min(count, inactiveIndicatorsCount);
                System.Array.Copy(inactiveIndicators, indicators, reusedCount);
                inactiveIndicatorsCount -= reusedCount;
                for (int i = 0; i < reusedCount; i++)
                    indicators[i].SetActive(true);
            }
            for (int i = reusedCount; i < count; i++)
            {
                GameObject indicator = Instantiate(summonIndicatorPrefab, summonIndicatorsContainer);
                indicator.SetActive(true);
                indicators[i] = indicator;
            }
            return indicators;
        }

        /// <summary>
        /// <para>Internal api.</para>
        /// </summary>
        /// <param name="indicatorGroup"></param>
        public void ReturnIndicatorGroup(PlayerSummonIndicatorGroup indicatorGroup)
        {
            ArrList.Add(ref inactiveIndicatorGroups, ref inactiveIndicatorGroupsCount, indicatorGroup);

            GameObject[] indicators = indicatorGroup.indicators;
            foreach (GameObject indicator in indicators)
                indicator.SetActive(false);
            ArrList.AddRange(ref inactiveIndicators, ref inactiveIndicatorsCount, indicators);
        }

        public override void SummonPlayers(PlayerSummonIndicatorGroup locations, CorePlayerData[] players, DataDictionary playersToExclude)
        {
            locations.HideDelayed(summonDelay + ExtraSummonDelayToCoverLatency);
            SendSummonIA(locations, players, playersToExclude);
        }

        private void SendSummonIA(PlayerSummonIndicatorGroup locations, CorePlayerData[] players, DataDictionary playersToExclude)
        {
            GameObject[] indicators = locations.indicators;
            int count = indicators.Length;
            lockstep.WriteSmallUInt((uint)count);
            lockstep.WriteVector3(locations.centerPosition);
            if (playersToExclude == null)
                for (int i = 0; i < count; i++)
                {
                    playerDataManager.WriteCorePlayerDataRef(players[i]);
                    lockstep.WriteVector3(indicators[i].transform.position);
                }
            else
            {
                int locationIndex = 0;
                int playerIndex = 0;
                while (true)
                {
                    CorePlayerData player = players[playerIndex++];
                    if (playersToExclude.ContainsKey(player))
                        continue;
                    playerDataManager.WriteCorePlayerDataRef(player);
                    lockstep.WriteVector3(indicators[locationIndex++].transform.position);
                    if (locationIndex == count)
                        break;
                }
            }
            lockstep.SendInputAction(summonIAId);
        }

        [HideInInspector][SerializeField] private uint summonIAId;
        [LockstepInputAction(nameof(summonIAId))]
        public void OnSummonIA()
        {
            if (!permissionManager.PlayerHasPermission(playerDataManager.SendingPlayerData, summonPlayersPDef))
                return;
            int count = (int)lockstep.ReadSmallUInt();
            Vector3 centerPosition = lockstep.ReadVector3();
            centerPosition.y = 0f;
            for (int i = 0; i < count; i++)
            {
                CorePlayerData player = playerDataManager.ReadCorePlayerDataRef();
                if (player == null || !player.isLocal)
                {
                    lockstep.ReadBytes(12, skip: true); // Skip the Vector3.
                    continue;
                }
                Vector3 summonPosition = lockstep.ReadVector3();
                Vector3 summonPositionOnYPlane = summonPosition;
                summonPositionOnYPlane.y = 0f;
                Quaternion summonRotation = Quaternion.LookRotation(summonPositionOnYPlane - centerPosition);
                EnqueueLocalSummon(summonPosition, summonRotation);
                return;
            }
        }

        private void EnqueueLocalSummon(Vector3 position, Quaternion rotation)
        {
            // TODO: Show hud, summon with delay.
        }
    }
}
