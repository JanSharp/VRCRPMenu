using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SortableScrollableRow : UdonSharpBehaviour
    {
        [System.NonSerialized] public int index;
        public GameObject rowGo;
        public RectTransform rowRect;
    }
}
