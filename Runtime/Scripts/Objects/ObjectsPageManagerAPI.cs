using UdonSharp;
using VRC.SDK3.Data;

namespace JanSharp
{
    [SingletonScript("92700c2678a68959e841d8c6e6e44bcb")] // Runtime/Prefabs/Managers/ObjectsPageManager.prefab
    public abstract class ObjectsPageManagerAPI : UdonSharpBehaviour
    {
        public abstract DataDictionary ObjectPrototypeNamesLut { get; }

        public abstract EntityPrototype[] ObjectPrototypesRaw { get; }
        public abstract int ObjectPrototypesCount { get; }
        public abstract EntityPrototype GetObjectPrototype(int index);
    }
}
