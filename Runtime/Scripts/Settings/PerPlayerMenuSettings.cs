using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PerPlayerMenuSettings : PlayerData
    {
        public override string PlayerDataInternalName => "jansharp.rp-menu-settings";
        public override string PlayerDataDisplayName => "Menu Settings";
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] private MenuSettingsManagerAPI menuSettingsManager;

        #region GameState
        [System.NonSerialized] public bool uiSoundsEnabled;
        [System.NonSerialized] public float uiSoundsVolume;
        [System.NonSerialized] public RPMenuDefaultPageType defaultPage;
        [System.NonSerialized] public MenuOpenCloseKeyBind menuOpenCloseKeyBind;
        [System.NonSerialized] public MenuPositionType menuPosition;
        #endregion

        public override void OnPlayerDataInit(bool isAboutToBeImported)
        {
            if (isAboutToBeImported
                && menuSettingsManager.OptionsFromExport.includeMenuSettings
                && menuSettingsManager.ImportOptions.includeMenuSettings)
            {
                return;
            }
            uiSoundsEnabled = menuSettingsManager.InitialUISoundsEnabled;
            uiSoundsVolume = menuSettingsManager.InitialUISoundsVolume;
            defaultPage = menuSettingsManager.InitialDefaultPage;
            menuOpenCloseKeyBind = menuSettingsManager.InitialMenuOpenCloseKeyBind;
            menuPosition = menuSettingsManager.InitialMenuPosition;
            if (core.isLocal) // Only the case for the very first client, during player data OnInit.
                ((Internal.MenuSettingsManager)menuSettingsManager).ResetLatencyStateToGameState(this, suppressEvents: true);
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return uiSoundsEnabled != menuSettingsManager.InitialUISoundsEnabled
                || !Mathf.Approximately(uiSoundsVolume, menuSettingsManager.InitialUISoundsVolume)
                || defaultPage != menuSettingsManager.InitialDefaultPage
                || menuOpenCloseKeyBind != menuSettingsManager.InitialMenuOpenCloseKeyBind
                || menuPosition != menuSettingsManager.InitialMenuPosition;
        }

        public override bool PersistPlayerDataInExport()
        {
            return menuSettingsManager.ExportOptions.includeMenuSettings && PersistPlayerDataWhileOffline();
        }

        private void WriteSettings()
        {
            lockstep.WriteFlags(uiSoundsEnabled);
            lockstep.WriteFloat(uiSoundsVolume);
            lockstep.WriteByte((byte)defaultPage);
            lockstep.WriteByte((byte)menuOpenCloseKeyBind);
            lockstep.WriteByte((byte)menuPosition);
        }

        private void ReadSettings(bool isImport, bool discard)
        {
            if (discard)
            {
                lockstep.ReadFlags(out bool discard1); // Cannot use 'out _'.
                lockstep.ReadFloat();
                lockstep.ReadBytes(3, skip: true);
                return;
            }
            lockstep.ReadFlags(out uiSoundsEnabled);
            uiSoundsVolume = lockstep.ReadFloat();
            defaultPage = (RPMenuDefaultPageType)lockstep.ReadByte();
            menuOpenCloseKeyBind = (MenuOpenCloseKeyBind)lockstep.ReadByte();
            menuPosition = (MenuPositionType)lockstep.ReadByte();
            if (core.isLocal)
                ((Internal.MenuSettingsManager)menuSettingsManager).ResetLatencyStateToGameState(this, suppressEvents: !isImport);
        }

        public override void Serialize(bool isExport)
        {
            if (!isExport || menuSettingsManager.ExportOptions.includeMenuSettings)
                WriteSettings();
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            if (!isImport)
                ReadSettings(isImport, discard: false);
            else if (menuSettingsManager.OptionsFromExport.includeMenuSettings)
                ReadSettings(isImport, discard: !menuSettingsManager.ImportOptions.includeMenuSettings);
        }
    }
}
