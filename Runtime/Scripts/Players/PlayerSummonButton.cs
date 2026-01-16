using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSummonButton : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private PlayerSelectionManager selectionManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerSummonManagerAPI summonManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;

        public RectTransform buttonRoot;
        public Button button;
        public Selectable label;
        public Transform popupLocation;
        public RectTransform confirmationPopup;

        [PermissionDefinitionReference(nameof(summonPlayersPDef))]
        public string summonPlayersPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition summonPlayersPDef;

        private bool localPlayerIsSelected;
        private int selectedCountExcludingLocalPlayer;
        private bool isShowingPreview;
        private PlayerSummonIndicatorGroup previewIndicators;
        private DataDictionary playersToExclude;

        public void OnSummonClick()
        {
            if (isShowingPreview)
                return;
            isShowingPreview = true;

            confirmationPopup.anchoredPosition = Vector2.zero;
            Vector2 sizeDelta = confirmationPopup.sizeDelta;
            sizeDelta.x = buttonRoot.rect.width;
            confirmationPopup.sizeDelta = sizeDelta;
            menuManager.ShowPopupAtCurrentPosition(confirmationPopup, this, nameof(OnPopupClosed), minDistanceFromPageEdge: 0f);

            VRCPlayerApi player = Networking.LocalPlayer;
            previewIndicators = summonManager.ShowIndicatorsInACircle(player.GetPosition(), player.GetRotation(), selectedCountExcludingLocalPlayer);
        }

        public void OnPopupClosed()
        {
            confirmationPopup.SetParent(popupLocation, worldPositionStays: false);
            isShowingPreview = false;
            if (previewIndicators != null)
            {
                previewIndicators.Hide();
                previewIndicators = null;
            }
        }

        public void OnConfirmSummonClick()
        {
            if (!isShowingPreview)
                return;
            if (playersToExclude == null)
            {
                playersToExclude = new DataDictionary();
                playersToExclude.Add(playerDataManager.LocalPlayerData, true);
            }
            summonManager.SummonPlayers(previewIndicators, selectionManager.selectedPlayers, localPlayerIsSelected ? playersToExclude : null);
            previewIndicators = null; // Before closing the popup.
            menuManager.ClosePopup(confirmationPopup, doCallback: true);
        }

        public override void InitializeInstantiated() { }

        public override void Resolve()
        {
            if (isShowingPreview && !summonPlayersPDef.valueForLocalPlayer)
                menuManager.ClosePopup(confirmationPopup, doCallback: true);
        }

        [PlayerSelectionEvent(PlayerSelectionEventType.OnOnePlayerSelectionChanged)]
        public void OnOnePlayerSelectionChanged() => OnSelectionChanged();

        [PlayerSelectionEvent(PlayerSelectionEventType.OnMultiplePlayerSelectionChanged)]
        public void OnMultiplePlayerSelectionChanged() => OnSelectionChanged();

        private void OnSelectionChanged()
        {
            UpdateButtonInteractable();
            if (isShowingPreview)
                UpdatePreview();
        }

        private void UpdateButtonInteractable()
        {
            localPlayerIsSelected = selectionManager.selectedPlayersLut.ContainsKey(playerDataManager.LocalPlayerData);
            selectedCountExcludingLocalPlayer = selectionManager.selectedPlayersCount;
            if (localPlayerIsSelected)
                selectedCountExcludingLocalPlayer--;
            bool interactable = selectedCountExcludingLocalPlayer != 0;
            button.interactable = interactable;
            label.interactable = interactable;
        }

        private void UpdatePreview()
        {
            if (previewIndicators.indicators.Length == selectedCountExcludingLocalPlayer)
                return;
            Vector3 position = previewIndicators.centerPosition;
            Quaternion rotation = previewIndicators.centerRotation;
            // If preview indicators have show and hide animations this will end up playing them. I do not mind.
            previewIndicators.Hide();
            previewIndicators = summonManager.ShowIndicatorsInACircle(position, rotation, selectedCountExcludingLocalPlayer);
        }
    }
}
