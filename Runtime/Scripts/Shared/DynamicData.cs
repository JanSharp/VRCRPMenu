using UnityEngine;

namespace JanSharp
{
    public abstract class DynamicData : SerializableWannaBeClass
    {
        public override bool SupportsImportExport => false;

        [HideInInspector][SerializeField][SingletonReference] protected PlayerDataManagerAPI playerDataManager;

        #region GameState
        [System.NonSerialized] public uint id;
        [System.NonSerialized] public string dataName;
        [System.NonSerialized] public bool isGlobal;
        [System.NonSerialized] public CorePlayerData owningPlayer;
        [System.NonSerialized] public bool livesInUndoStack;
        [System.NonSerialized] public uint overwrittenAtTick;
        #endregion

        public override void Serialize(bool isExport)
        {
            lockstep.WriteSmallUInt(id);
            lockstep.WriteString(dataName);
            lockstep.WriteFlags(isGlobal, livesInUndoStack);
            playerDataManager.WriteCorePlayerDataRef(owningPlayer);
            if (livesInUndoStack)
                lockstep.WriteSmallUInt(overwrittenAtTick);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            id = lockstep.ReadSmallUInt();
            dataName = lockstep.ReadString();
            lockstep.ReadFlags(out isGlobal, out livesInUndoStack);
            owningPlayer = playerDataManager.ReadCorePlayerDataRef();
            if (livesInUndoStack)
                overwrittenAtTick = lockstep.ReadSmallUInt();
        }
    }
}
