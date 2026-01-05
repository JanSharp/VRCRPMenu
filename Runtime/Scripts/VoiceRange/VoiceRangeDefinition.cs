using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceRangeDefinition : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;
        [HideInInspector][SerializeField][SingletonReference] private VoiceRangeManagerAPI voiceRangeManager;

        // All of this is read only at runtime.

        [System.NonSerialized] public int index;
        [System.NonSerialized] public uint bitMaskFlag;
        [Tooltip("Used for remapping in imports and by voice range toggles.")]
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

        [PermissionDefinitionReference(nameof(permissionDef), Optional = true)]
        public string permissionAsset; // A guid.
        [HideInInspector] public PermissionDefinition permissionDef;

        [Header("Lower number means higher priority", order = 0)]
        [Space(-10, order = 1)]
        [Header("Audio Zones have priority 1000", order = 2)]
        public int audioSettingPriority = 2000;

        public bool LocalPlayerHasPermission
            => permissionDef == null
            || permissionDef.valueForLocalPlayer;

        public bool PlayerHasPermission(CorePlayerData player)
            => permissionDef == null
            || permissionManager.PlayerHasPermission(player, permissionDef);

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (permissionDef.valueForLocalPlayer || !voiceRangeManager.IsInitialized)
                return;
            VoiceRangePlayerData localPlayer = voiceRangeManager.LocalPlayer;
            if (localPlayer.latencyVoiceRangeIndex == index)
                voiceRangeManager.SendSetVoiceRangeIndexIA(localPlayer, voiceRangeManager.DefaultVoiceRangeIndex);
        }
    }
}
