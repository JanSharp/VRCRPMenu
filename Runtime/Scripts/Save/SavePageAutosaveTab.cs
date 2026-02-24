using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

// NOTE: Unfortunately this file contains significant copy paste from the GameStatesUI.cs file from the lockstep package.

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SavePageAutosaveTab : SavePageTab
    {
        [SerializeField][HideInInspector][SingletonReference] private LockstepAPI lockstep;
        [SerializeField][HideInInspector][SingletonReference] private UpdateManager updateManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        [SerializeField] private GameObject autosaveTimerGo;
        [SerializeField] private Slider autosaveTimerSlider;
        [SerializeField] private TextMeshProUGUI autosaveTimerText;

        [SerializeField] private LockstepOptionsEditorUI autosaveOptionsUI;
        [SerializeField] private Toggle autosaveUsesExportOptionsToggle;
        [SerializeField] private Toggle autosaveUsesExportOptionsLinkedToggle;
        [SerializeField] private TMP_InputField autosaveIntervalField;
        [SerializeField] private Slider autosaveIntervalSlider;
        [SerializeField] private Toggle autosavesEnabledToggle;
        [SerializeField] private Toggle autosavesEnabledLinkedToggle;
        [SerializeField] private TextMeshProUGUI autosaveEnabledToggleInfo;

        private float minAutosaveInterval = 1f;
        private float defaultAutosaveInterval = 5f;
        private float autosaveInterval = 5f;

        private bool tabIsShown = false;
        private LabelWidgetData autosaveUsingExportInfoLabel;

        private bool anySupportImportExport;
        private bool isInitialized = false;

        [SerializeField] private SavePageExportTab exportTab;
        private LockstepGameStateOptionsData[] autosaveOptions;
        private bool AutosaveUsesExportOptions => autosaveOptions == exportTab.ExportOptions;

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public override void OnMenuManagerStart()
        {
            base.OnMenuManagerStart();
            autosaveOptionsUI.Init();
            UpdateAutosaveToggle();
            StartStopAutosaveTimerUpdateLoop();

            // The autosave options are set to a reference to the export options by default, ensure the UI matches.
            autosaveUsesExportOptionsToggle.SetIsOnWithoutNotify(true);
            autosaveUsesExportOptionsLinkedToggle.SetIsOnWithoutNotify(true);

            minAutosaveInterval = autosaveIntervalSlider.minValue;
            defaultAutosaveInterval = autosaveIntervalSlider.value;
            autosaveInterval = defaultAutosaveInterval;
            autosaveIntervalField.SetTextWithoutNotify(((int)autosaveInterval).ToString());
        }

        // These four events get raised in this order.

        public void OnExportSettingsChanged()
        {
            if (AutosaveUsesExportOptions && autosavesEnabledToggle.isOn)
                lockstep.ExportOptionsForAutosave = autosaveOptions; // exportOptions == autosaveOptions
        }

        public override void OnPageBecameActive()
        {
        }

        public override void OnTabGotShown()
        {
            tabIsShown = true;
            ShowAutosaveOptionsEditor();
            StartStopAutosaveTimerUpdateLoop();
        }

        public override void OnTabGotHidden()
        {
            tabIsShown = false;
            HideAutosaveOptionsEditor();
            autosaveOptionsUI.Clear();
            autosaveOptionsUI.Draw(); // Return widgets to the pool.
            StartStopAutosaveTimerUpdateLoop();
        }

        public override void OnPageBecameInactive()
        {
        }

        public void OnAutosaveUsesExportOptionsToggleValueChanged()
        {
            bool isOn = autosaveUsesExportOptionsToggle.isOn;
            autosaveUsesExportOptionsLinkedToggle.SetIsOnWithoutNotify(isOn);
            if (!isInitialized)
                return;
            if (AutosaveUsesExportOptions == isOn)
                return;
            if (AutosaveUsesExportOptions)
                autosaveOptions = lockstep.CloneAllOptions(exportTab.ExportOptions);
            else
            {
                foreach (LockstepGameStateOptionsData options in autosaveOptions)
                    if (options != null)
                        options.DecrementRefsCount();
                autosaveOptions = exportTab.ExportOptions;
            }
            HideAutosaveOptionsEditor();
            ShowAutosaveOptionsEditor();
        }

        private void ShowAutosaveOptionsEditor()
        {
            autosaveOptionsUI.Clear();
            exportTab.AddGameStatesToExportToInfoWidget(autosaveOptionsUI);
            lockstep.ShowExportOptionsEditor(autosaveOptionsUI, autosaveOptions);
            if (autosaveUsesExportOptionsToggle.isOn)
            {
                autosaveOptionsUI.Root.Interactable = false;
                autosaveUsingExportInfoLabel = autosaveUsingExportInfoLabel ?? autosaveOptionsUI.WidgetManager.NewLabel(
                    "Currently using export options for autosaves. Modifying is disabled to prevent "
                        + "accidentally confusing exports vs autosaves.");
                autosaveOptionsUI.Info.AddChild(autosaveUsingExportInfoLabel);
                autosaveOptionsUI.Info.FoldedOut = true; // Ensure this info message is visible.
            }
            autosaveOptionsUI.Draw();
        }

        private void HideAutosaveOptionsEditor()
        {
            if (!AutosaveUsesExportOptions)
                lockstep.UpdateAllCurrentExportOptionsFromWidgets();
            lockstep.HideExportOptionsEditor();
            autosaveOptionsUI.Root.Interactable = true;
            if (autosavesEnabledToggle.isOn)
                lockstep.ExportOptionsForAutosave = autosaveOptions;
        }

        public void OnAutosavesEnabledToggleValueChanged()
        {
            bool isOn = autosavesEnabledToggle.isOn;
            autosavesEnabledLinkedToggle.SetIsOnWithoutNotify(isOn);
            if (!isInitialized)
                return;
            lockstep.ExportOptionsForAutosave = isOn
                ? lockstep.GetAllCurrentExportOptions(weakReferences: true)
                : null;
        }

        public void OnAutosaveIntervalFieldValueChanged()
        {
            if (int.TryParse(autosaveIntervalField.text, out int autosaveIntervalMinutes))
                autosaveInterval = (float)autosaveIntervalMinutes;
            else
                autosaveInterval = defaultAutosaveInterval;
            if (autosaveInterval < minAutosaveInterval)
            {
                autosaveInterval = minAutosaveInterval;
                autosaveIntervalField.SetTextWithoutNotify(((int)autosaveInterval).ToString());
            }
            autosaveIntervalSlider.SetValueWithoutNotify(autosaveInterval);
            SendApplyAutosaveIntervalDelayed();
        }

        public void OnAutosaveIntervalSliderChanged()
        {
            autosaveInterval = autosaveIntervalSlider.value;
            autosaveIntervalField.SetTextWithoutNotify(((int)autosaveInterval).ToString());
            SendApplyAutosaveIntervalDelayed();
        }

        private void SendApplyAutosaveIntervalDelayed()
        {
            applyAutosaveIntervalDelayedCounter++;
            SendCustomEventDelayedSeconds(nameof(ApplyAutosaveIntervalDelayed), 2f);
            if (applyAutosaveIntervalDelayedCounter == 1)
                lockstep.StartScopedAutosavePause();
        }

        private int applyAutosaveIntervalDelayedCounter = 0;
        public void ApplyAutosaveIntervalDelayed()
        {
            if (applyAutosaveIntervalDelayedCounter == 0 || (--applyAutosaveIntervalDelayedCounter) != 0)
                return;
            lockstep.StopScopedAutosavePause();
            lockstep.AutosaveIntervalSeconds = autosaveInterval * 60f; // Raises OnAutosaveIntervalSecondsChanged.
        }

        private void StartStopAutosaveTimerUpdateLoop()
        {
            if (tabIsShown && lockstep.HasExportOptionsForAutosave)
            {
                autosaveTimerGo.SetActive(true);
                updateManager.Register(this);
                CustomUpdate();
            }
            else
            {
                autosaveTimerGo.SetActive(false);
                updateManager.Deregister(this);
            }
        }

        public void CustomUpdate()
        {
            if (!lockstep.HasExportOptionsForAutosave)
                return; // It will get deregistered next frame. There's a frame delay for the changed event.

            if (lockstep.IsAutosavePaused)
            {
                autosaveTimerText.text = $"Autosave Paused";
                return;
            }

            float seconds = lockstep.SecondsUntilNextAutosave;
            float interval = lockstep.AutosaveIntervalSeconds + 0.0625f; // Prevent division by 0.
            autosaveTimerSlider.value = (interval - seconds) / interval;
            int hours = (int)(seconds / 3600f);
            seconds -= hours * 3600;
            int minutes = (int)(seconds / 60f);
            seconds -= minutes * 60;
            if (hours != 0)
                autosaveTimerText.text = $"Autosave in {hours}h {minutes + (seconds > 0f ? 1 : 0)}m";
            else if (minutes != 0)
                autosaveTimerText.text = $"Autosave in {minutes + (seconds > 0f ? 1 : 0)}m";
            else
                autosaveTimerText.text = $"Autosave in {(int)seconds}s";
        }

        private bool CanAutosave() => isInitialized && anySupportImportExport && !lockstep.IsImporting;

        private void UpdateAutosaveToggle()
        {
            autosavesEnabledToggle.interactable = CanAutosave();
            autosaveEnabledToggleInfo.text = !isInitialized ? "Please wait, loading..."
                : !anySupportImportExport ? "None Support Exporting"
                : lockstep.IsImporting ? "Please wait, importing..."
                : "";
        }

        private void OnInitialized()
        {
            isInitialized = true;
            anySupportImportExport = lockstep.GameStatesSupportingImportExportCount != 0;
            autosaveOptions = exportTab.ExportOptions;
            UpdateAutosaveToggle();
            StartStopAutosaveTimerUpdateLoop();
            // Things that other scripts could have already modified but were ignored because lockstep was not
            // initialized yet.
            OnAutosavesEnabledToggleValueChanged();
            OnAutosaveUsesExportOptionsToggleValueChanged();
        }

        [LockstepEvent(LockstepEventType.OnInitFinished, Order = 1)] // After SavePageExportTab.
        public void OnInitFinished() => OnInitialized();

        [LockstepEvent(LockstepEventType.OnPostClientBeginCatchUp, Order = 1)] // After SavePageExportTab.
        public void OnPostClientBeginCatchUp() => OnInitialized();

        [LockstepEvent(LockstepEventType.OnExportStart)]
        public void OnExportStart() => UpdateAutosaveToggle();

        [LockstepEvent(LockstepEventType.OnExportFinished)]
        public void OnExportFinished() => UpdateAutosaveToggle();

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart() => UpdateAutosaveToggle();

        [LockstepEvent(LockstepEventType.OnImportFinished)]
        public void OnImportFinished() => UpdateAutosaveToggle();

        [LockstepEvent(LockstepEventType.OnExportOptionsForAutosaveChanged)]
        public void OnExportOptionsForAutosaveChanged() => StartStopAutosaveTimerUpdateLoop();

        [LockstepEvent(LockstepEventType.OnAutosaveIntervalSecondsChanged)]
        public void OnAutosaveIntervalSecondsChanged()
        {
            autosaveInterval = Mathf.Floor(lockstep.AutosaveIntervalSeconds / 60f);
            autosaveIntervalField.SetTextWithoutNotify(((int)autosaveInterval).ToString());
            autosaveIntervalSlider.SetValueWithoutNotify(autosaveInterval);
            StartStopAutosaveTimerUpdateLoop();
        }
    }
}
