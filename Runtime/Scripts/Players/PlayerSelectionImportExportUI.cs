using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSelectionImportExportUI : DynamicDataImportExportOptionsUI
    {
        protected override string ToggleLabel => "Player Selection Groups";
    }
}
