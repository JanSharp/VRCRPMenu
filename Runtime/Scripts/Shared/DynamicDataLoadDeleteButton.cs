using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DynamicDataLoadDeleteButton : DynamicDataPopupListButton
    {
        [System.NonSerialized] public bool markedForDeletion;
        public DynamicDataLoadPopup popupScript;
        public override DynamicDataPopupList PopupScript => popupScript;
        public Selectable labelSelectable;
        public Selectable deleteIconSelectable;
        public Selectable undeleteIconSelectable;
        public GameObject deleteButtonRootGo;

        public void OnLoadClick() => popupScript.OnLoadButtonClick(this);

        public void OnDeleteClick() => popupScript.OnDeleteButtonClick(this);
    }
}
