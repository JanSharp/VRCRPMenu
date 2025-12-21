using TMPro;
using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendPermissionGroupButton : UdonSharpBehaviour
    {
        [System.NonSerialized] public PermissionGroup permissionGroup;
        [System.NonSerialized] public string sortablePermissionGroupName;
        public PlayersBackendPage page;
        public Image selectedImage;
        public TextMeshProUGUI groupNameLabel;

        public void OnClick() => page.OnPermissionGroupPopupButtonClick(this);
    }
}
