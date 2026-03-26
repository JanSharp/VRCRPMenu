using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsResolver : PermissionResolverForGameState
    {
        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;
        [HideInInspector][SerializeField][SingletonReference] private PermissionManagerAPI permissionManager;

        [PermissionDefinitionReference(nameof(useFlyingPDef))]
        public string useFlyingPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition useFlyingPDef;

        [PermissionDefinitionReference(nameof(useNoClipPDef))]
        public string useNoClipPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition useNoClipPDef;

        public override void ResolveAll(PermissionsPlayerData player)
        {
            bool hasFlying = permissionManager.PlayerHasPermission(player.core, useFlyingPDef);
            bool hasNoClip = permissionManager.PlayerHasPermission(player.core, useNoClipPDef);
            if (hasFlying == hasNoClip) // When having both or neither permissions, no need to switch type.
                return;
            NoClipSettingsPlayerData noClipPlayer = noClipSettingsManager.GetNoClipSettingsPlayerData(player.core);
            NoClipFlyingType flyingType = noClipPlayer.noClipFlyingType;
            if (!hasFlying && flyingType == NoClipFlyingType.Flying)
                noClipSettingsManager.SetNoClipFlyingTypeInGS(noClipPlayer, NoClipFlyingType.NoClip);
            else if (!hasNoClip && flyingType == NoClipFlyingType.NoClip)
                noClipSettingsManager.SetNoClipFlyingTypeInGS(noClipPlayer, NoClipFlyingType.Flying);
        }
    }
}
