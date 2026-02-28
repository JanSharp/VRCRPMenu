using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoClipSettingsPlayerData : PlayerData
    {
        public override string PlayerDataInternalName => "jansharp.rp-menu-no-clip-settings";
        public override string PlayerDataDisplayName => "No Clip Settings";
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] private NoClipSettingsManagerAPI noClipSettingsManager;

        #region GameState
        [System.NonSerialized] public bool noClipEnabled;
        /// <summary>
        /// <para>Meters per second.</para>
        /// </summary>
        [System.NonSerialized] public float noClipSpeed;
        #endregion

        public override void OnPlayerDataInit(bool isAboutToBeImported)
        {
            if (isAboutToBeImported
                && noClipSettingsManager.OptionsFromExport.includeNoClipSettings
                && noClipSettingsManager.ImportOptions.includeNoClipSettings)
            {
                return;
            }
            noClipEnabled = noClipSettingsManager.InitialNoClipEnabled;
            noClipSpeed = noClipSettingsManager.InitialNoClipSpeed;
            if (core.isLocal) // Only the case for the very first client, during player data OnInit.
                ((Internal.NoClipSettingsManager)noClipSettingsManager).ResetLatencyStateToGameState(this, suppressEvents: true);
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return noClipEnabled != noClipSettingsManager.InitialNoClipEnabled
                || noClipSpeed != noClipSettingsManager.InitialNoClipSpeed;
        }

        public override bool PersistPlayerDataInExport()
        {
            return noClipSettingsManager.ExportOptions.includeNoClipSettings && PersistPlayerDataWhileOffline();
        }

        private void WriteSettings()
        {
            lockstep.WriteFlags(noClipEnabled);
            lockstep.WriteFloat(noClipSpeed);
        }

        private void ReadSettings(bool isImport, bool discard)
        {
            if (discard)
            {
                lockstep.ReadFlags(out bool discard1); // Cannot use 'out _'.
                lockstep.ReadFloat();
                return;
            }
            lockstep.ReadFlags(out noClipEnabled);
            noClipSpeed = lockstep.ReadFloat();
            if (core.isLocal)
                ((Internal.NoClipSettingsManager)noClipSettingsManager).ResetLatencyStateToGameState(this, suppressEvents: !isImport);
        }

        public override void Serialize(bool isExport)
        {
            if (!isExport || noClipSettingsManager.ExportOptions.includeNoClipSettings)
                WriteSettings();
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            if (!isImport)
                ReadSettings(isImport, discard: false);
            else if (noClipSettingsManager.OptionsFromExport.includeNoClipSettings)
                ReadSettings(isImport, discard: !noClipSettingsManager.ImportOptions.includeNoClipSettings);
        }
    }
}
