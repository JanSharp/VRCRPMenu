using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    public enum RPMenuTeleportEventType
    {
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalPlayerTeleported,
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnRPMenuTeleportUndoRedoStateChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RPMenuTeleportEventAttribute : CustomRaisedEventBaseAttribute
    {
        /// <summary>
        /// <para>The method this attribute gets applied to must be public.</para>
        /// <para>The name of the function this attribute is applied to must have the exact same name as the
        /// name of the <paramref name="eventType"/>.</para>
        /// <para>Event registration is performed at OnBuild, which is to say that scripts with these kinds of
        /// event handlers must exist in the scene at build time, any runtime instantiated objects with these
        /// scripts on them will not receive these events.</para>
        /// <para>Disabled scripts still receive events.</para>
        /// </summary>
        /// <param name="eventType">The event to register this function as a listener to.</param>
        public RPMenuTeleportEventAttribute(RPMenuTeleportEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("3b1d00e6074519a39b78235f631308a1")] // Runtime/Prefabs/Managers/RPMenuTeleportManager.prefab
    public abstract class RPMenuTeleportManagerAPI : UdonSharpBehaviour
    {
        public const float DesiredDistanceFromOtherPlayers = 1.25f;
        public const float MinDistanceFromOtherPlayers = 0.75f;
        public const float MaxRelativeDownwardsDistance = 0.4f;
        public const float SafetyDistanceFromGround = 0.015f;
        public const float SafetyDistanceFromWalls = 0.1f;

        public abstract bool HasUndoData { get; }
        /// <summary>
        /// <para>When <see langword="false"/> it means "is at redo able location".</para>
        /// </summary>
        public abstract bool IsAtUndoAbleLocation { get; }
        public abstract float UndoAbleActionTakenAtTime { get; }

        public abstract bool RedoAbleLocationIsPlayer { get; }
        public abstract CorePlayerData RedoAblePlayer { get; }
        public abstract Vector3 RedoAbleDesiredRelativeDirection { get; }
        public abstract Vector3 RedoAblePosition { get; }
        public abstract Quaternion RedoAbleRotation { get; }

        public abstract Vector3 UndoAblePosition { get; }
        public abstract Quaternion UndoAbleRotation { get; }

        public abstract LayerMask LocalPlayerCollidingLayers { get; }
        public abstract void TeleportToPlayer(CorePlayerData otherPlayer, Vector3 desiredRelativeDirection, bool recordUndo = false);
        public abstract void TeleportTo(Vector3 position, Quaternion rotation, bool recordUndo = false);
        public abstract void UndoTeleport();
        public abstract void RedoTeleport();

        /// <summary>
        /// <para>Handles quaternions where their forward vector is pointing straight up or down.</para>
        /// </summary>
        /// <returns>A quaternion purely rotating around the Y axis. If the given <paramref name="rotation"/>
        /// was upside down, the result does not reflect as such. The "up" of the resulting rotation is always
        /// equal to <see cref="Vector3.up"/>.</returns>
        public abstract Quaternion ProjectOntoYPlane(Quaternion rotation);

        /// <summary>
        /// <para><see cref="VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint"/> is bugged,
        /// see this canny: https://feedback.vrchat.com/udon/p/teleporting-the-player-to-the-same-rotation-they-were-already-sometimes-introduc</para>
        /// <para><see cref="VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint"/> is bugged,
        /// see this canny: https://feedback.vrchat.com/udon/p/teleport-sometimes-gets-stuck-on-geometry</para>
        /// <para>The latter makes it unusable for no-clip. Even doing an "align player" teleport beforehand
        /// to theoretically move the player into the collider, followed by an "align room" teleport does not
        /// work, it has the exact same issue.</para>
        /// <para>Not just no-clip though, this can happen when teleporting through colliders even in "normal"
        /// use cases of the teleport function.</para>
        /// <para>Thus the only option is to use "align player" teleports, and deal with the former mentioned
        /// bug.</para>
        /// <para>Note that when not dealing with the bug, so just using a single normal "align player"
        /// teleport call for no-clip causes players in VR to spin when looking up/down and then looking left
        /// or right.</para>
        /// <para>Using <see cref="VRCPlayerApi.GetRotation"/> before and after a teleport call to then
        /// calculate a rotation difference which gets applied to a consecutive teleport call mostly works
        /// around this issue. Works for desktop, mostly works in half body though it sometimes causes
        /// intentional head movement to get undone, however in full body it causes nearly all horizontal
        /// head rotation to get cancelled out, such that looking left or right while using no-clip keeps the
        /// view point pointed straight as though the head did not move at all.</para>
        /// <para>Doing almost the exact same thing, however instead of using
        /// <see cref="VRCPlayerApi.GetRotation"/>, getting the head tracking data and using that to calculate
        /// a rotation difference that was induced by the teleport mostly solves this full body issue. There
        /// might just be a few jitters left or right that can be noticed when looking up or down while in
        /// full body VR.</para>
        /// <para>Turning the head upside down can cause the player to get turned around 180 degrees due to
        /// the projection of the head rotation onto the Y plane. Does not seem consistent, but luckily hardly
        /// anybody does that.</para>
        /// <para>And lastly another oddity that can happen is when turning the head in a circle rather
        /// quickly, some of that rotation can also get cancelled out, which is presumably jarring. It seems
        /// rare however.</para>
        /// </summary>
        public abstract void MoveAndRetainHeadRotation(Vector3 movement);
    }
}
