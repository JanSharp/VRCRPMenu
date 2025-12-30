using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayersBackendRow : SortableScrollableRow
    {
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
        public Transform overriddenDisplayNameCaretParent;
        public TMP_InputField characterNameField;
        public Transform characterNameCaretParent;
        public RectTransform permissionGroupRect;
        public TextMeshProUGUI permissionGroupLabel;
        public Transform permissionGroupPopupLocation;
        public Button deleteButton;
        public Selectable deleteLabel;
        public Transform confirmDeletePopupLocation;
        public Image activeRowHighlightImage;
        public GameObject overriddenDisplayNameRoot;
        public GameObject characterNameRoot;
        public GameObject permissionGroupRoot;
        public GameObject deleteRoot;

        public void OnOverriddenDisplayNameChanged() => page.OnOverriddenDisplayNameChanged(this);
        public void OnCharacterNameChanged() => page.OnCharacterNameChanged(this);
        public void OnPermissionGroupClick() => page.OnPermissionGroupClick(this);
        public void OnDeleteClick() => page.OnDeleteClick(this);

        private void Start()
        {
            SendCustomEvent(nameof(StartDelayed));
        }

        public void StartDelayed()
        {
            DeactivateCaret(overriddenDisplayNameCaretParent);
            DeactivateCaret(characterNameCaretParent);
        }

        private void DeactivateCaret(Transform parent)
        {
            if (parent.childCount == 0)
                return;
            Transform caret = parent.GetChild(0);
            if (caret.name == "Caret")
                caret.gameObject.SetActive(false);
        }
    }
}
