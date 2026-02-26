using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CustomLocationImportExportUI : DynamicDataImportExportOptionsUI
    {
        protected override string ToggleLabel => "Custom Locations";
    }
}
