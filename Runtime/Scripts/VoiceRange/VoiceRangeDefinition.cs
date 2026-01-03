using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeDefinition : UdonSharpBehaviour
    {
        // All of this is read only at runtime.

        [System.NonSerialized] public int index;
        [System.NonSerialized] public uint bitMaskFlag;
        [Tooltip("Used for remapping in imports.")]
        public string internalName;
        public Color color;
        [Space]
        // https://creators.vrchat.com/worlds/udon/players/player-audio/
        [Range(0f, 24f)]
        public float gain = 15f;
        [Range(0f, 1_000_000f)]
        public float nearRange = 0f;
        [Range(0f, 1_000_000f)]
        public float farRange = 25f;
        [Range(0f, 1_000f)]
        public float volumetricRange = 0f;
        public bool lowPass = false;
        [Space]
        public bool showInWorldByDefault;
        public bool showInHUDByDefault;
    }
}
