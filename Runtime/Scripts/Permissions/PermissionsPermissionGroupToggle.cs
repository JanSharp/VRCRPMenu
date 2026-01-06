using TMPro;
using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PermissionsPermissionGroupToggle : UdonSharpBehaviour
    {
        public PermissionsPage page;
        public Toggle toggle;
        public TextMeshProUGUI groupNameLabel;
        [System.NonSerialized] public PermissionGroup permissionGroup;
        [System.NonSerialized] public string sortablePermissionGroupName;

        public void OnValueChanged()
        {
            if (toggle.isOn)
                page.SetActivePermissionGroupToggle(this);
            else
                toggle.SetIsOnWithoutNotify(true);
        }
    }
}
