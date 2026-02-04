using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GMProxyDefinition : UdonSharpBehaviour
    {
        [Tooltip("Used by spawn buttons.")]
        public string internalName;
        public Color color;
        public string entityPrototypeName;
    }
}
