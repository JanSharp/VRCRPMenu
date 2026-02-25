using System.Text;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

// NOTE: Unfortunately this file contains significant copy paste from the GameStatesUI.cs file from the lockstep package.

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SavePageImportTab : SavePageTab
    {
        [SerializeField][HideInInspector][SingletonReference] private LockstepAPI lockstep;

        [SerializeField] private TMP_InputField serializedInputField;
        [SerializeField] private LockstepOptionsEditorUI importOptionsUI;
        [SerializeField] private Button confirmImportButton;
        [SerializeField] private Selectable confirmImportButtonTextSelectable;
        [SerializeField] private TextMeshProUGUI confirmImportButtonText;

        private bool tabIsShown = false;

        private bool anySupportImportExport;
        private bool isInitialized = false;

        /// <summary>
        /// <para><see cref="LockstepImportedGS"/>[]</para>
        /// </summary>
        private object[][] importedGameStates;
        private System.DateTime exportDate;
        private string exportWorldName;
        private string exportName;

        /// <summary>
        /// <para>Keys: <see cref="string"/> <see cref="LockstepGameState.GameStateInternalName"/>,<br/>
        /// Values: <see cref="LockstepGameStateOptionsData"/> <c>importOptions</c>.</para>
        /// </summary>
        private DataDictionary importOptions;
        private bool anyImportedGSHasNoErrors;

        [PermissionDefinitionReference(nameof(importGameStatesPDef))]
        public string importGameStatesPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition importGameStatesPDef;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public override void OnMenuManagerStart()
        {
            base.OnMenuManagerStart();
            importOptionsUI.Init();
            UpdateImportButton();
        }

        // These four events get raised in this order.

        public override void OnPageBecameActive()
        {
        }

        public override void OnTabGotShown()
        {
            tabIsShown = true;
            base.OnTabGotShown();
        }

        public override void OnTabGotHidden()
        {
            tabIsShown = false;
            base.OnTabGotHidden();
            // Do not call HideImportOptionsEditor or anything else in here, as that would mean rebuilding the
            // editor in OnTabGotShown. Unlike the export and autosave tabs we can expect the user not to
            // paste something into the input field, close the menu and keep it closed forever. The user is
            // likely to actually go through with the import, or cancel by going to a different page, rather
            // quickly. Therefore there is no need to worry about any systems keeping the import options
            // editor up to date while the menu is actually not shown, which would be a waste of performance.
            // It would still be a waste here, however it is likely to only be for a very short time.
        }

        public override void OnPageBecameInactive()
        {
            ResetImport();
            importOptionsUI.Draw(); // Return widgets to the pool.
        }

        // Cannot use EndEdit because that simply does not get raised in VRChat. We only get value changed.
        // Which is oh so very great in the editor because value changed gets raised for every character that
        // gets pasted in, while in VRChat it'll only get raised once.
        // This handler is expensive, so rerunning it for every character causes exponentially long lag
        // spikes. In the editor. You know, the thing we use for testing.
        // https://vrchat.canny.io/sdk-bug-reports/p/worlds-316-vrcinputfield-inputfield-no-longer-sends-onendedit-event
        public void OnImportSerializedTextValueChanged()
        {
            if (onImportSerializedTextValueChangedDelayedQueued)
                return;
            onImportSerializedTextValueChangedDelayedQueued = true;
            SendCustomEventDelayedFrames(nameof(OnImportSerializedTextValueChangedDelayed), 1);
        }

        private bool onImportSerializedTextValueChangedDelayedQueued = false;
        public void OnImportSerializedTextValueChangedDelayed()
        {
            onImportSerializedTextValueChangedDelayedQueued = false;
            if (!isInitialized)
                return;

            if (tabIsShown)
                tabRoot.SetActive(false); // Reduce UI layout overhead and prevent nonsensical generic value editor UI animations.
            // Reset regardless, because 2 consecutive valid yet different imports could be pasted in.
            ResetImport(leaveInputFieldUntouched: true);

            string importString = serializedInputField.text;
            if (importString == "")
            {
                if (tabIsShown)
                    tabRoot.SetActive(true);
                return;
            }
            importedGameStates = lockstep.ImportPreProcess(
                importString,
                out exportDate,
                out exportWorldName,
                out exportName);
            if (importedGameStates == null)
            {
                importOptionsUI.Info.AddChild(importOptionsUI.WidgetManager.NewLabel("Malformed or invalid data.").StdMoveWidget());
                importOptionsUI.Draw();
                if (tabIsShown)
                    tabRoot.SetActive(true);
                return;
            }

            LabelWidgetData mainInfoLabel = importOptionsUI.Info.AddChild(importOptionsUI.WidgetManager.NewLabel(""));

            string importedGameStatesMsg = BuildImportedGameStatesMsg(out int canImportCount, out bool anyWarnings);
            if (anyWarnings)
                importOptionsUI.Info.FoldedOut = true; // Else retain state, don't set to false.

            FoldOutWidgetData gsFoldOut = importOptionsUI.Info.AddChild(
                importOptionsUI.WidgetManager.NewFoldOutScope("Game States", foldedOut: anyWarnings));
            gsFoldOut.AddChild(importOptionsUI.WidgetManager.NewLabel(importedGameStatesMsg).StdMoveWidget());
            gsFoldOut.DecrementRefsCount();
            anyImportedGSHasNoErrors = canImportCount != 0;

            int cannotImportCount = importedGameStates.Length - canImportCount;
            mainInfoLabel.Label = $"Can import {(cannotImportCount == 0 ? "all " : "")}{canImportCount}"
                + (cannotImportCount == 0 ? "" : $", cannot import {cannotImportCount}")
                + $"\n<nobr><size=90%>{exportName ?? "<i>unnamed</i>"} "
                + $"<size=60%>(from <size=75%>{exportWorldName}<size=60%>, "
                + $"{exportDate.ToLocalTime():yyyy-MM-dd HH:mm})</nobr>";
            mainInfoLabel.DecrementRefsCount();

            lockstep.AssociateImportOptionsWithImportedGameStates(importedGameStates, importOptions);
            lockstep.ShowImportOptionsEditor(importOptionsUI, importedGameStates);
            importOptionsUI.Draw();

            UpdateImportButton();

            if (tabIsShown)
                tabRoot.SetActive(true);
        }

        private string BuildImportedGameStatesMsg(out int canImportCount, out bool anyWarnings)
        {
            DataDictionary importedGSByInternalName = new DataDictionary();
            foreach (object[] importedGS in importedGameStates)
                importedGSByInternalName.Add(LockstepImportedGS.GetInternalName(importedGS), new DataToken(importedGS));

            canImportCount = 0;
            anyWarnings = false;

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
                if (!importedGSByInternalName.TryGetValue(gameState.GameStateInternalName, out DataToken importedGSToken))
                {
                    if (!gameState.GameStateSupportsImportExport)
                        sb.Append(" - <color=#888888>does not support import</color>");
                    else
                    {
                        sb.Append(" - <color=#ffaaaa>not in imported data</color>");
                        anyWarnings = true;
                    }
                }
                else
                {
                    object[] importedGS = (object[])importedGSToken.Reference;
                    string errorMsg = LockstepImportedGS.GetErrorMsg(importedGS);
                    if (errorMsg != null)
                    {
                        sb.Append(" - <color=#ffaaaa>");
                        sb.Append(errorMsg);
                        sb.Append("</color>");
                        anyWarnings = true;
                    }
                    else
                    {
                        canImportCount++;
                        sb.Append(" - <color=#99ccff>supports import</color>");
                    }
                }
            }

            foreach (object[] importedGS in importedGameStates)
            {
                LockstepGameState gameState = LockstepImportedGS.GetGameState(importedGS);
                if (gameState != null)
                    continue;

                if (isFirstLine)
                    isFirstLine = false;
                else
                    sb.Append('\n'); // AppendLine could add \r\n. \r is outdated. \n is the future.

                sb.Append(LockstepImportedGS.GetDisplayName(importedGS));
                sb.Append(" - <color=#ffaaaa>");
                sb.Append(LockstepImportedGS.GetErrorMsg(importedGS));
                sb.Append("</color>");
                anyWarnings = true;
            }

            return sb.ToString();
        }

        private bool CanImport() => isInitialized && anySupportImportExport && anyImportedGSHasNoErrors && !lockstep.IsImporting;

        private void UpdateImportButton()
        {
            bool canImport = CanImport();
            confirmImportButton.interactable = canImport;
            confirmImportButtonTextSelectable.interactable = canImport;
            confirmImportButtonText.text = !isInitialized ? "Loading..."
                : !anySupportImportExport ? "None Support Importing"
                : lockstep.IsImporting ? "Importing..."
                : "Import";
        }

        public void OnImportClick()
        {
            if (!CanImport())
            {
                Debug.LogError("[RPMenu] Through means meant to be impossible the import button has been "
                    + "pressed when it cannot actually import. Someone messed with something.");
                return;
            }
            if (!importGameStatesPDef.valueForLocalPlayer) // Client side only check (though the tab should be hidden anyway),
                return; // lockstep does not provide a way to do "server" side checks for this.
            lockstep.UpdateAllCurrentImportOptionsFromWidgets();
            lockstep.StartImport(importedGameStates, exportDate, exportWorldName, exportName);
            ResetImport(leaveInputFieldUntouched: false, alreadyUpdatedOptions: true);
            importOptionsUI.Draw(); // Return widgets to the pool.
        }

        private void ResetImport(bool leaveInputFieldUntouched = false, bool alreadyUpdatedOptions = false)
        {
            if (!leaveInputFieldUntouched)
                serializedInputField.SetTextWithoutNotify("");

            if (importedGameStates != null)
            {
                if (!alreadyUpdatedOptions)
                    lockstep.UpdateAllCurrentImportOptionsFromWidgets(); // Remember options even when not confirming an import.
                lockstep.HideImportOptionsEditor();
                lockstep.CleanupImportedGameStatesData(importedGameStates);
            }
            importedGameStates = null; // Free for GC.
            importOptionsUI.Clear();
            importOptionsUI.Info.FoldedOut = true;
            anyImportedGSHasNoErrors = false;
            UpdateImportButton();
        }

        private void OnInitialized()
        {
            isInitialized = true;
            anySupportImportExport = lockstep.GameStatesSupportingImportExportCount != 0;
            importOptions = lockstep.GetNewImportOptions();
            UpdateImportButton();
            // Things that other scripts could have already modified but were ignored because lockstep was not
            // initialized yet.
            OnImportSerializedTextValueChanged();
        }

        [LockstepEvent(LockstepEventType.OnInitFinished)]
        public void OnInitFinished() => OnInitialized();

        [LockstepEvent(LockstepEventType.OnPostClientBeginCatchUp)]
        public void OnPostClientBeginCatchUp() => OnInitialized();

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart() => UpdateImportButton();

        [LockstepEvent(LockstepEventType.OnImportFinished)]
        public void OnImportFinished() => UpdateImportButton();
    }
}
