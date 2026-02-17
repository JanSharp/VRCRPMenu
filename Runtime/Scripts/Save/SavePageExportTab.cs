using System.Text;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

// NOTE: Unfortunately this file contains significant copy paste from the GameStatesUI.cs file from the lockstep package.

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SavePageExportTab : SavePageTab
    {
        [SerializeField][HideInInspector][SingletonReference] private LockstepAPI lockstep;

        [SerializeField] private LockstepOptionsEditorUI exportOptionsUI;
        [SerializeField] private TMP_InputField exportNameField;
        [SerializeField] private Button confirmExportButton;
        [SerializeField] private Selectable confirmExportButtonTextSelectable;
        [SerializeField] private TextMeshProUGUI confirmExportButtonText;

        [SerializeField] private TextMeshProUGUI exportedDataSizeText;
        private string exportedDataSizeTextFormat;
        [SerializeField] private TMP_InputField serializedOutputField;

        private bool anySupportImportExport;
        private bool isInitialized = false;

        private bool waitingForExportToFinish = false;

        private LockstepGameStateOptionsData[] exportOptions;
        public LockstepGameStateOptionsData[] ExportOptions => exportOptions;

        [PermissionDefinitionReference(nameof(exportGameStatesPDef))]
        public string exportGameStatesPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition exportGameStatesPDef;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public override void OnMenuManagerStart()
        {
            base.OnMenuManagerStart();
            exportedDataSizeTextFormat = exportedDataSizeText.text;
            SetExportedDataSizeText(0, 0);
            exportOptionsUI.Init();
            UpdateExportButton();
        }

        // These four events get raised in this order.

        public override void OnPageBecameActive()
        {
        }

        public override void OnTabGotShown()
        {
            exportOptionsUI.Clear();
            AddGameStatesToExportToInfoWidget(exportOptionsUI);
            lockstep.ShowExportOptionsEditor(exportOptionsUI, exportOptions);
            exportOptionsUI.Draw();
        }

        public override void OnTabGotHidden()
        {
            lockstep.UpdateAllCurrentExportOptionsFromWidgets();
            lockstep.HideExportOptionsEditor();
            // TODO
            // if (!AutosaveUsesExportOptions && autosavesEnabledToggle.isOn)
            //     lockstep.ExportOptionsForAutosave = exportOptions; // exportOptions == autosaveOptions
            exportOptionsUI.Clear();
            exportOptionsUI.Draw(); // Return widgets to the pool.
        }

        public override void OnPageBecameInactive()
        {
            waitingForExportToFinish = false;
            SetExportedDataSizeText(0, 0);
            serializedOutputField.text = "";
        }

        private string BuildGameStatesToExportMsg()
        {
            if (lockstep.AllGameStatesCount == 0)
                return "<size=80%>There are no game states in this world";

            StringBuilder sb = new StringBuilder();
            sb.Append("<size=80%>");
            bool isFirstLine = true;

            foreach (LockstepGameState gameState in lockstep.AllGameStates)
            {
                if (isFirstLine)
                    isFirstLine = false;
                else
                    sb.Append('\n'); // AppendLine could add \r\n. \r is outdated. \n is the future.

                sb.Append(gameState.GameStateDisplayName);
                sb.Append(gameState.GameStateSupportsImportExport
                    ? " - <color=#99ccff>supports export</color>"
                    : " - <color=#888888>does not support export</color>");
            }

            return sb.ToString();
        }

        public void AddGameStatesToExportToInfoWidget(LockstepOptionsEditorUI optionsUI)
        {
            FoldOutWidgetData gsFoldOut = optionsUI.Info.AddChild(optionsUI.WidgetManager.NewFoldOutScope("Game States", true));
            gsFoldOut.AddChild(optionsUI.WidgetManager.NewLabel(BuildGameStatesToExportMsg()).StdMoveWidget());
            gsFoldOut.DecrementRefsCount();
        }

        private bool CanExport() => isInitialized && anySupportImportExport && !lockstep.IsExporting && !lockstep.IsImporting;

        private void UpdateExportButton()
        {
            bool canExport = CanExport();
            confirmExportButton.interactable = canExport;
            confirmExportButtonTextSelectable.interactable = canExport;
            confirmExportButtonText.text = !isInitialized ? "Loading..."
                : !anySupportImportExport ? "None Support Exporting"
                : lockstep.IsExporting ? "Exporting..."
                : lockstep.IsImporting ? "Importing..."
                : "Export";
        }

        public void OnExportClick()
        {
            if (!CanExport())
            {
                Debug.LogError("[RPMenu] Through means meant to be impossible the export button has been "
                    + "pressed when it cannot actually export. Someone messed with something.");
                return;
            }
            if (!exportGameStatesPDef.valueForLocalPlayer) // Client side only check (though the tab should be hidden anyway),
                return; // lockstep does not provide a way to do "server" side checks for this.

            // It is a single line field so I would expect newlines to be impossible, however since I cannot
            // trust it, just in case they do exist they get removed, because according to lockstep's api they
            // are invalid.
            string exportName = exportNameField.text.Trim().Replace('\n', ' ').Replace('\r', ' ');
            if (exportName == "")
                exportName = null;
            if (lockstep.StartExport(exportName, lockstep.GetAllCurrentExportOptions(weakReferences: true)))
                waitingForExportToFinish = true;
            else
                Debug.LogError("[RPMenu] Export failed to start, this is supposed to be impossible.");
        }

        private void OnInitialized()
        {
            isInitialized = true;
            anySupportImportExport = lockstep.GameStatesSupportingImportExportCount != 0;
            exportOptions = lockstep.GetNewExportOptions();
            UpdateExportButton();
        }

        [LockstepEvent(LockstepEventType.OnInitFinished)]
        public void OnInitFinished() => OnInitialized();

        [LockstepEvent(LockstepEventType.OnPostClientBeginCatchUp)]
        public void OnPostClientBeginCatchUp() => OnInitialized();

        [LockstepEvent(LockstepEventType.OnExportStart)]
        public void OnExportStart() => UpdateExportButton();

        [LockstepEvent(LockstepEventType.OnExportFinished)]
        public void OnExportFinished()
        {
            UpdateExportButton();
            if (!waitingForExportToFinish)
                return;

            waitingForExportToFinish = false;
            string result = lockstep.ExportResult;
            SetExportedDataSizeText(lockstep.ExportByteCount, result.Length);
            serializedOutputField.text = result;
        }

        private void SetExportedDataSizeText(int byteCount, int characterCount)
        {
            exportedDataSizeText.text = string.Format(
                exportedDataSizeTextFormat,
                StringUtil.FormatNumberWithSpaces(byteCount),
                StringUtil.FormatNumberWithSpaces(characterCount));
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart() => UpdateExportButton();

        [LockstepEvent(LockstepEventType.OnImportFinished)]
        public void OnImportFinished() => UpdateExportButton();
    }
}
