using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CustomLocation : DynamicData
    {
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        #region GameState
        [System.NonSerialized] public Vector3 position;
        [System.NonSerialized] public Quaternion rotation;
        #endregion

        public override void Serialize(bool isExport)
        {
            base.Serialize(isExport);
            lockstep.WriteVector3(position);
            lockstep.WriteQuaternion(rotation);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            base.Deserialize(isImport, importedDataVersion);
            position = lockstep.ReadVector3();
            rotation = lockstep.ReadQuaternion();
        }
    }
}
