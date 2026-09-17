using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsPageManager : ObjectsPageManagerAPI
    {
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;

        /// <summary>
        /// <para><see cref="string"/> entityPrototypeName => <see langword="true"/></para>
        /// </summary>
        private DataDictionary objectPrototypeNamesLut = new DataDictionary();
        public override DataDictionary ObjectPrototypeNamesLut => objectPrototypeNamesLut;

        private EntityPrototype[] objectPrototypes = new EntityPrototype[ArrList.MinCapacity];
        private int objectPrototypesCount = 0;
        public override EntityPrototype[] ObjectPrototypesRaw => objectPrototypes;
        public override int ObjectPrototypesCount => objectPrototypesCount;
        public override EntityPrototype GetObjectPrototype(int index) => objectPrototypes[index];

        private void Start()
        {
            EntityPrototype[] prototypes = entitySystem.EntityPrototypes;
            foreach (EntityPrototype prototype in prototypes)
            {
                string[] classNames = prototype.ExtensionDataClassNames;
                if (System.Array.IndexOf(classNames, nameof(ObjectEntityExtensionData)) != -1)
                {
                    objectPrototypeNamesLut.Add(prototype.PrototypeName, true);
                    ArrList.Add(ref objectPrototypes, ref objectPrototypesCount, prototype);
                }
            }
        }
    }
}
