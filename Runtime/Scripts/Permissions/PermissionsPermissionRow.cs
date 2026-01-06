using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PermissionsPermissionRow : UdonSharpBehaviour
    {
        public TextMeshProUGUI label;
        public Toggle toggle;
        public Toggle linkedToggle;
        [HideInInspector] public PermissionDefinition permissionDef;

        public void OnValueChanged()
        {
            linkedToggle.SetIsOnWithoutNotify(toggle.isOn);
        }

        public void SetIsOnWithoutNotify(bool value)
        {
            toggle.SetIsOnWithoutNotify(value);
            linkedToggle.SetIsOnWithoutNotify(value);
        }
    }
}
