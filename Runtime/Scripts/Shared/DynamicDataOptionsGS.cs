using UnityEngine;

namespace JanSharp
{
    // Must be its own game state because the player selection game state must load after player data
    // while options must load before in order to be available for custom player data.
    [LockstepGameStateDependency(typeof(PlayerDataManagerAPI), SelfLoadsBeforeDependency = true)]
    public abstract class DynamicDataOptionsGS : LockstepGameState
    {
        public override bool GameStateSupportsImportExport => true;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;
        [SerializeField] private DynamicDataImportExportOptionsUI exportUI;
        [SerializeField] private DynamicDataImportExportOptionsUI importUI;
        public override LockstepGameStateOptionsUI ExportUI => exportUI;
        public override LockstepGameStateOptionsUI ImportUI => importUI;

        public DynamicDataImportExportOptions ExportOptions => (DynamicDataImportExportOptions)OptionsForCurrentExport;
        public DynamicDataImportExportOptions ImportOptions => (DynamicDataImportExportOptions)OptionsForCurrentImport;
        private DynamicDataImportExportOptions optionsFromExport;
        public DynamicDataImportExportOptions OptionsFromExport => optionsFromExport;

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
            if (!isExport)
                return;
            lockstep.WriteCustomClass(exportOptions);
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
            if (!isImport)
                return null;
            optionsFromExport = (DynamicDataImportExportOptions)lockstep.ReadCustomClass(nameof(DynamicDataImportExportOptions));
            return null;
        }

        [LockstepEvent(LockstepEventType.OnImportFinished, Order = 10000)]
        public virtual void OnImportFinished()
        {
            if (!IsPartOfCurrentImport)
                return;
            optionsFromExport.DecrementRefsCount();
            optionsFromExport = null;
        }
    }
}
