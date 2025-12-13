using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendRow : UdonSharpBehaviour
    {
        [System.NonSerialized] public int index;
        [System.NonSerialized] public string sortablePlayerName;
        [System.NonSerialized] public string sortableOverriddenDisplayName;
        [System.NonSerialized] public string sortableCharacterName;
        [System.NonSerialized] public string sortablePermissionGroupName;
        [System.NonSerialized] public RPPlayerData rpPlayerData;
        [System.NonSerialized] public PermissionsPlayerData permissionsPlayerData;
        public PlayersBackendPage page;
        public TextMeshProUGUI playerNameLabel;
        public TMP_InputField overriddenDisplayNameField;
        public TextMeshProUGUI overriddenDisplayNameLabel;
        public TMP_InputField characterNameField;
        public RectTransform permissionGroupRect;
        public TextMeshProUGUI permissionGroupLabel;
        public Transform permissionGroupPopupLocation;
        public Button deleteButton;
        public Selectable deleteLabel;
        public Transform confirmDeletePopupLocation;
        public Image activeRowHighlightImage;

        public void OnOverriddenDisplayNameChanged() => page.OnOverriddenDisplayNameChanged(this);
        public void OnCharacterNameChanged() => page.OnCharacterNameChanged(this);
        public void OnPermissionGroupClick() => page.OnPermissionGroupClick(this);
        public void OnDeleteClick() => page.OnDeleteClick(this);
    }
}
