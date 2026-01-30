using UdonSharp;

namespace JanSharp
{
    [SingletonScript("a208660966802b7a19b48fcd51d32afa")] // Runtime/Prefabs/Managers/NoClipMovement.prefab
    public abstract class NoClipMovementAPI : UdonSharpBehaviour
    {
        /// <summary>
        /// <para>Meters per second.</para>
        /// </summary>
        public abstract float Speed { get; set; }
        public abstract bool IsNoClipActive { get; set; }
        public abstract void IncrementAvoidTeleporting();
        public abstract void DecrementAvoidTeleporting();
    }
}
