using UnityEngine;

namespace JanSharp
{
    public abstract class DynamicDataManager : LockstepGameState
    {
        public override bool GameStateSupportsImportExport => false;
        public override LockstepGameStateOptionsUI ExportUI => null;
        public override LockstepGameStateOptionsUI ImportUI => null;

        public abstract string DynamicDataClassName { get; }

        [HideInInspector][SerializeField][SingletonReference] protected PlayerDataManagerAPI playerDataManager;

        #region GameState
        private DynamicData[] globalDynamicData = new DynamicData[ArrList.MinCapacity];
        private int globalDynamicDataCount = 0;
        #endregion

        private DynamicData[] overwriteUndoStack = new DynamicData[ArrList.MinCapacity];
        private int overwriteUndoStackCount = 0;

        [PlayerDataEvent(PlayerDataEventType.OnRegisterCustomPlayerData)]
        public void OnRegisterCustomPlayerData()
        {
            playerDataManager.RegisterCustomPlayerDataDynamic(DynamicDataClassName);
        }

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
            lockstep.WriteSmallUInt((uint)globalDynamicDataCount);
            for (int i = 0; i < globalDynamicDataCount; i++)
                lockstep.WriteCustomClass(globalDynamicData[i]);
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
            globalDynamicDataCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref globalDynamicData, globalDynamicDataCount);
            for (int i = 0; i < globalDynamicDataCount; i++)
                globalDynamicData[i] = (DynamicData)lockstep.ReadCustomClass(DynamicDataClassName);
            return null;
        }
    }
}
