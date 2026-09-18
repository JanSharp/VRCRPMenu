using UdonSharp;
using UnityEngine;
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

        public abstract LayerMask ObjectsCollisionLayers { get; }
        public abstract float MaxRaycastDistance { get; }
        public abstract Transform LaserPointerRoot { get; }
        public abstract EntityTransformGizmoBridge EntityTransformGizmo { get; }

        /// <summary>
        /// <para>Newly created previews will be set to inactive. It is the job of other systems to set
        /// preview instances to inactive when done using them.</para>
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public abstract GameObject GetCreationPreviewInstanceForPrototype(EntityPrototype prototype);
    }
}
