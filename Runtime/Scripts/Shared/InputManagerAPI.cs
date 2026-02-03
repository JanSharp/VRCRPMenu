using UdonSharp;
using VRC.SDKBase;

namespace JanSharp
{
    [SingletonScript("f6232818b2c225a8dbe62019e7941d18")] // Runtime/Prefabs/Managers/InputManager.prefab
    public abstract class InputManagerAPI : UdonSharpBehaviour
    {
        public abstract void DetermineHandUsedForClick(UdonSharpBehaviour callbackInst, string callbackEventName, object callbackCustomData);
        public abstract VRCPlayerApi.TrackingDataType DeterminedHand { get; }
        public abstract object CallbackCustomData { get; }
    }
}
