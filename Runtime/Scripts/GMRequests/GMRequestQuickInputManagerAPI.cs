using UdonSharp;

namespace JanSharp
{
    public enum GMRequestsQuickInputEventType
    {
        /// <summary>
        /// <para>Raised when starting to hold inputs.</para>
        /// <para><see cref="GMRequestQuickInputManagerAPI.IsInProgress"/> is <see langword="true"/> in this
        /// event.</para>
        /// <para><see cref="GMRequestQuickInputManagerAPI.Progress"/> is <c>0f</c> in this event.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestQuickInputBegin,
        /// <summary>
        /// <para>Raised every frame while <see cref="GMRequestQuickInputManagerAPI.IsInProgress"/> is
        /// <see langword="true"/>.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestQuickInputUpdate,
        /// <summary>
        /// <para>Raised when inputs have been held for the full duration.</para>
        /// <para><see cref="GMRequestQuickInputManagerAPI.IsInProgress"/> is <see langword="false"/> in this
        /// event.</para>
        /// <para><see cref="GMRequestQuickInputManagerAPI.Progress"/> is <c>1f</c> in this event.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestQuickInputCompleted,
        /// <summary>
        /// <para>Raised when inputs have been let go and progress fell down to zero.</para>
        /// <para><see cref="GMRequestQuickInputManagerAPI.IsInProgress"/> is <see langword="false"/> in this
        /// event.</para>
        /// <para><see cref="GMRequestQuickInputManagerAPI.Progress"/> is <c>0f</c> in this event.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnGMRequestQuickInputAborted,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class GMRequestsQuickInputEventAttribute : CustomRaisedEventBaseAttribute
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
        public GMRequestsQuickInputEventAttribute(GMRequestsQuickInputEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("0570cfc327166669f8076bf07c86b568")] // Runtime/Prefabs/Managers/GMRequestQuickInputManager.prefab
    public abstract class GMRequestQuickInputManagerAPI : UdonSharpBehaviour
    {
        public abstract bool IsInProgress { get; }
        public abstract float Progress { get; }
        public abstract void IncrementIgnoreInput();
        public abstract void DecrementIgnoreInput();
    }
}
