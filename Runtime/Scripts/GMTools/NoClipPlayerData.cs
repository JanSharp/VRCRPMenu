using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipPlayerData : PlayerData
    {
        public override string PlayerDataInternalName => "jansharp.rp-menu-no-clip";
        public override string PlayerDataDisplayName => "No Clip Settings";
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] private NoClipManagerAPI noClipManager;

        #region GameState
        [System.NonSerialized] public bool noClipEnabled;
        [System.NonSerialized] public float noClipSpeed;
        #endregion

        public override void OnPlayerDataInit(bool isAboutToBeImported)
        {
            noClipEnabled = noClipManager.InitialNoClipEnabled;
            noClipSpeed = noClipManager.InitialNoClipSpeed;
            if (core.isLocal) // Only the case for the very first client, during player data OnInit.
                ((Internal.NoClipManager)noClipManager).ResetLatencyStateToGameState(this, suppressEvents: true);
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return noClipEnabled != noClipManager.InitialNoClipEnabled
                || noClipSpeed != noClipManager.InitialNoClipSpeed;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(noClipEnabled);
            lockstep.WriteFloat(noClipSpeed);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(out noClipEnabled);
            noClipSpeed = lockstep.ReadFloat();
            if (core.isLocal)
                ((Internal.NoClipManager)noClipManager).ResetLatencyStateToGameState(this, suppressEvents: !isImport);
        }
    }
}
