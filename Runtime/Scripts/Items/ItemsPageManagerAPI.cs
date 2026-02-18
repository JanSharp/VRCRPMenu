using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [SingletonScript("74781a00b310a9b848f04b3f0c276d2d")] // Runtime/Prefabs/Managers/ItemsPageManager.prefab
    public abstract class ItemsPageManagerAPI : UdonSharpBehaviour
    {
        /// <summary>
        /// <para>A direct reference to the internal array, which is an <see cref="ArrList"/>, which is to say
        /// that the <see cref="System.Array.Length"/> of this array cannot be trusted.</para>
        /// <para>It being an <see cref="ArrList"/> also implies that fetching this property and keeping a
        /// reference to the returned value can end up referring to a stale no longer used array in the
        /// future, if the arrays has been grown internally since fetching it.</para>
        /// <para>The actual amount of elements used of this array is defined via
        /// <see cref="ItemPrototypesCount"/>.</para>
        /// <para>Game state safe, including order.</para>
        /// </summary>
        public abstract EntityPrototype[] ItemPrototypesRaw { get; }
        public abstract int ItemPrototypesCount { get; }
        public abstract EntityPrototype GetItemPrototype(int index);

        public abstract void CreateItem(EntityPrototype prototype, Vector3 position, Quaternion rotation, float scale);
    }
}
