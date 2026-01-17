using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    public enum RPMenuTeleportEventType
    {
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
    }
}
