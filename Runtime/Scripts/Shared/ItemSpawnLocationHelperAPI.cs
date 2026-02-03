using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [SingletonScript("4a998117f6673abf9861e0bbc135d3b8")] // Runtime/Prefabs/Managers/ItemSpawnLocationHelper.prefab
    public abstract class ItemSpawnLocationHelperAPI : UdonSharpBehaviour
    {
        public abstract void DetermineItemSpawnLocation(UdonSharpBehaviour callbackInst, string callbackEventName, object callbackCustomData);
        public abstract Vector3 DeterminedPosition { get; }
        public abstract Quaternion DeterminedRotation { get; }
        public abstract object CallbackCustomData { get; }
    }
}
