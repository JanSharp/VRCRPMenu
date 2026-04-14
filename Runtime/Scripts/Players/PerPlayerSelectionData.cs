using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PerPlayerSelectionData : PerPlayerDynamicData
    {
        public override string PlayerDataInternalName => "jansharp.rp-menu-player-selection";
        public override string PlayerDataDisplayName => "Player Selection";

        [HideInInspector][SerializeField][SingletonReference] protected PlayerSelectionManager selectionManager;
        public override DynamicDataManager DataManager => selectionManager;

        public override string DynamicDataClassName => nameof(PlayerSelectionGroup);

        #region GameState
        /// <summary>
        /// <para>Actually never negative.</para>
        /// </summary>
        [System.NonSerialized] public int selectedInGlobalGroupsCount = 0;
        /// <summary>
        /// <para>Actually never negative.</para>
        /// </summary>
        [System.NonSerialized] public int selectedInPerPlayerGroupsCount = 0;
        #endregion

        public override bool WannaBeClassSupportsPooling => true;
        public override void ResetWannaBeClassToDefault()
        {
            base.ResetWannaBeClassToDefault();
            selectedInGlobalGroupsCount = 0;
            selectedInPerPlayerGroupsCount = 0;
        }

        public override bool PersistPlayerDataWhileOffline()
        {
            return base.PersistPlayerDataWhileOffline()
                || selectedInGlobalGroupsCount != 0
                || selectedInPerPlayerGroupsCount != 0;
        }

        public override bool PersistPlayerDataInExport()
        {
            DynamicDataImportExportOptions exportOptions = selectionManager.optionsGS.ExportOptions;
            return base.PersistPlayerDataInExport()
                || exportOptions.includeGlobal && selectedInGlobalGroupsCount != 0
                || exportOptions.includePerPlayer && selectedInPerPlayerGroupsCount != 0;
        }

        private void WriteGlobalCount()
        {
            lockstep.WriteSmallUInt((uint)selectedInGlobalGroupsCount);
        }

        private void ReadGlobalCount(bool discard)
        {
            if (discard)
                lockstep.ReadSmallUInt();
            else
                selectedInGlobalGroupsCount = (int)lockstep.ReadSmallUInt();
        }

        private void WritePerPlayerCount()
        {
            lockstep.WriteSmallUInt((uint)selectedInPerPlayerGroupsCount);
        }

        private void ReadPerPlayerCount(bool discard)
        {
            if (discard)
                lockstep.ReadSmallUInt();
            else
                selectedInPerPlayerGroupsCount = (int)lockstep.ReadSmallUInt();
        }

        public override void Serialize(bool isExport)
        {
            base.Serialize(isExport);

            if (!isExport || selectionManager.optionsGS.ExportOptions.includeGlobal)
                WriteGlobalCount();

            if (!isExport || selectionManager.optionsGS.ExportOptions.includePerPlayer)
                WritePerPlayerCount();
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            base.Deserialize(isImport, importedDataVersion);

            if (!isImport)
                ReadGlobalCount(discard: false);
            else if (selectionManager.optionsGS.OptionsFromExport.includeGlobal)
                ReadGlobalCount(discard: !selectionManager.optionsGS.ImportOptions.includeGlobal);

            if (!isImport)
                ReadPerPlayerCount(discard: false);
            else if (selectionManager.optionsGS.OptionsFromExport.includePerPlayer)
                ReadPerPlayerCount(discard: !selectionManager.optionsGS.ImportOptions.includePerPlayer);
        }
    }
}
