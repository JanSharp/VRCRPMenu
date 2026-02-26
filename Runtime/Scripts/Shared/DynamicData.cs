using UnityEngine;

namespace JanSharp
{
    public abstract class DynamicData : SerializableWannaBeClass
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [HideInInspector][SerializeField][SingletonReference] protected PlayerDataManagerAPI playerDataManager;

        #region GameState
        [System.NonSerialized] public uint id;
        [System.NonSerialized] public string dataName;
        [System.NonSerialized] public bool isGlobal;
        [System.NonSerialized] public CorePlayerData owningPlayer;
        #endregion

        public override void Serialize(bool isExport)
        {
            if (!isExport)
                lockstep.WriteSmallUInt(id);
            lockstep.WriteString(dataName);
            lockstep.WriteFlags(isGlobal);
            playerDataManager.WriteCorePlayerDataRef(owningPlayer);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            if (!isImport)
                id = lockstep.ReadSmallUInt();
            dataName = lockstep.ReadString();
            lockstep.ReadFlags(out isGlobal);
            owningPlayer = playerDataManager.ReadCorePlayerDataRef();
        }
    }
}
