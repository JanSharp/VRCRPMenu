using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DynamicDataOverwriteButton : DynamicDataPopupListButton
    {
        public DynamicDataSavePopup popupScript;
        public override DynamicDataPopupList PopupScript => popupScript;

        public void OnClick() => popupScript.OnOverwriteButtonClick(this);
    }
}
