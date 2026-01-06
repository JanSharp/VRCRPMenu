using TMPro;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PermissionsPermissionRow : UdonSharpBehaviour
    {
        public TextMeshProUGUI label;
        [HideInInspector] public PermissionDefinition permissionDef;

        public void OnValueChanged()
        {

        }
    }
}
