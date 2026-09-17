using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [DisallowMultipleComponent]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectEntityHighlight : CustomInteractableBase
    {
        public override bool CanInteract() => false;
        public override float GetEffectiveVRReach() => 0f;
    }
}
