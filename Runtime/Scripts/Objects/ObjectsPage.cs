using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp
{
    public enum ObjectsPageMode
    {
        Idle,
        Creating,
        Editing,
        Deleting,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectsPage : PermissionResolver
    {
        [HideInInspector][SerializeField][SingletonReference] private LockstepAPI lockstep;
        [HideInInspector][SerializeField][SingletonReference] private PlayersBackendManagerAPI playersBackendManager;
        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;
        [HideInInspector][SerializeField][SingletonReference] private ObjectsPageManagerAPI objectsPageManager;
        [HideInInspector][SerializeField][SingletonReference] private ObjectsFavoritesManager objectsFavoritesManager;
        [HideInInspector][SerializeField][SingletonReference] private InputManagerAPI inputManager;
        [HideInInspector][SerializeField][SingletonReference] private MenuInputHandler menuInputHandler;
        [HideInInspector][SerializeField][SingletonReference] private CustomInteractablesManagerAPI interactables;
        [HideInInspector][SerializeField][SingletonReference] private EntitySystem entitySystem;
        [HideInInspector][SerializeField][SingletonReference] private UpdateManager updateManager;
        [HideInInspector][SerializeField][FindInParent] private MenuManagerAPI menuManager;
        /// <summary>
        /// <para>Used by <see cref="UpdateManager"/>.</para>
        /// </summary>
        private int customUpdateInternalIndex;

        public ObjectsList rowsList;
        public ObjectsRow rowPrefabScript;

        public Transform popupsParent;

        private LayerMask objectsCollisionLayers; // Fetched from ObjectsPageManager.
        private float maxRaycastDistance; // Fetched from ObjectsPageManager.
        private Transform laserPointerRoot; // Fetched from ObjectsPageManager.
        private GameObject laserPointerRootGo;
        private EntityTransformGizmoBridge entityTransformGizmo; // Fetched from ObjectsPageManager.

        private ObjectsPageMode currentMode;
        private RectTransform activePopup;
        private VRCPlayerApi.TrackingDataType activeHand;
        private HandType activeHandType;
        private void SetActiveHand(VRCPlayerApi.TrackingDataType hand)
        {
            activeHand = hand;
            activeHandType = hand == VRCPlayerApi.TrackingDataType.RightHand ? HandType.RIGHT : HandType.LEFT;
        }

        private VRCPlayerApi.TrackingDataType determinedHand;
        private object determinedHandCallbackCustomData;

        private float lastInputUseEventTime;

        #region Creating
        [Header("Creating")]
        public RectTransform creatingPopup;
        public TextMeshProUGUI creatingHeaderLabel;
        private string creatingHeaderLabelFormat;
        public Toggle alignmentTargetSurfaceNormalToggle;
        public Toggle alignmentWorldUpToggle;
        public Toggle referenceYourDirectionToggle;
        public Toggle referenceWorldForwardToggle;
        public ObjectsFacingOptions facingOptions;
        public Toggle uponPlacePlaceMoreToggle;
        public Toggle uponPlaceSwitchToEditToggle;
        public Toggle uponPlaceClosePopupToggle;
        private ObjectsRow activeRow;
        private GameObject currentCreationPreviewGo;
        #endregion

        private ObjectEntityExtension pointedAtObject = null;
        private Transform lastPointedAtTransform = null; // For optimization.

        #region Editing
        [Header("Editing")]
        public RectTransform editingPopup;
        private EntityData editingEntityData = null;
        private bool waitingForEditingEntityToGetCreated;
        public TextMeshProUGUI editingHeaderLabel;
        private string editingHeaderLabelFormat;
        public GameObject editingInfoWhileDeselected;
        public GameObject editingInfoWhileSelected;
        public Button editingDeselectCurrentButton;
        public Selectable editingDeselectCurrentButtonLabel;
        public Button editingDuplicateButton;
        public Selectable editingDuplicateButtonLabel;
        #endregion

        #region Deleting
        [Header("Deleting")]
        public RectTransform deletingPopup;
        #endregion

        [Header("Permissions")]

        [PermissionDefinitionReference(nameof(useObjectsPDef))]
        public string useObjectsPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition useObjectsPDef;

        [PermissionDefinitionReference(nameof(viewObjectCategoryPDef))]
        public string viewObjectCategoryPermissionAsset; // A guid.
        [HideInInspector][SerializeField] private PermissionDefinition viewObjectCategoryPDef;

        // private bool isInitialized = false;

        private RPPlayerData localPlayer;
        private VRCPlayerApi localPlayerApi;
        private uint localPlayerId;
        private bool isInVR;

        #region Event Listeners

        [PlayerDataEvent(PlayerDataEventType.OnLocalPlayerDataAvailable)]
        public void OnLocalPlayerDataAvailable()
        {
            localPlayer = playersBackendManager.GetRPPlayerData(playerDataManager.LocalPlayerData);
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuManagerStart)]
        public void OnMenuManagerStart()
        {
            localPlayerApi = Networking.LocalPlayer;
            localPlayerId = (uint)localPlayerApi.playerId;
            isInVR = localPlayerApi.IsUserInVR();

            objectsCollisionLayers = objectsPageManager.ObjectsCollisionLayers;
            maxRaycastDistance = objectsPageManager.MaxRaycastDistance;
            laserPointerRoot = objectsPageManager.LaserPointerRoot;
            laserPointerRootGo = laserPointerRoot.gameObject;
            entityTransformGizmo = objectsPageManager.EntityTransformGizmo;

            creatingHeaderLabelFormat = creatingHeaderLabel.text;
            editingHeaderLabelFormat = editingHeaderLabel.text;
        }

        [MenuManagerEvent(MenuManagerEventType.OnMenuActivePageChanged)]
        public void OnMenuActivePageChanged()
        {
            EnterIdleMode();
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            // isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            if (!lockstep.IsContinuationFromPrevFrame)
                rowsList.Initialize();
            RebuildRows();
            if (lockstep.FlaggedToContinueNextFrame)
                return;
            // isInitialized = true;
        }

        [LockstepEvent(LockstepEventType.OnImportStart)]
        public void OnImportStart()
        {
            EnterIdleMode();
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp)]
        public void OnImportFinishingUp()
        {
            if (playerDataManager.IsPartOfCurrentImport)
                UpdateAllFavorites();
        }

        public void CustomUpdate()
        {
            UpdateCurrentMode();
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (currentMode == ObjectsPageMode.Idle || !value)
                return;

            // NOTE: Comment and input deduplication copied from CustomInteractHandManager.cs
            // Ignore multiple InputUse events in the same frame... because for some unexplainable reason
            // VRChat is raising the InputUse event twice when I click the mouse button once.
            float timeTime = Time.time;
            if ((isInVR && args.handType != activeHandType) || lastInputUseEventTime == timeTime)
                return;
            lastInputUseEventTime = timeTime;

            HandleInputUseForCurrentMode();
        }

        #endregion

        #region PermissionResolution

        public override void InitializeInstantiated() { }

        public override void ResolveAll()
        {
            if (!useObjectsPDef.valueForLocalPlayer)
                EnterIdleMode();

            bool viewObjectCategoryValue = viewObjectCategoryPDef.valueForLocalPlayer;

            rowsList.SortOnPermissionChange(viewObjectCategoryValue);

            bool objectCategoryChanged = rowPrefabScript.categoryRoot.activeSelf != viewObjectCategoryValue;
            rowPrefabScript.categoryRoot.SetActive(viewObjectCategoryValue);

            if (!objectCategoryChanged)
                return;

            for (int i = 0; i < 2; i++)
            {
                ObjectsRow[] rows = i == 0 ? rowsList.Rows : rowsList.UnusedRows;
                int rowsCount = i == 0 ? rowsList.RowsCount : rowsList.UnusedRowsCount;
                for (int j = 0; j < rowsCount; j++)
                    rows[j].categoryRoot.SetActive(viewObjectCategoryValue);
            }
        }

        #endregion

        #region RowsManagement

        private bool TryGetRow(uint entityPrototypeId, out ObjectsRow row) => rowsList.TryGetRow(entityPrototypeId, out row);

        private void RebuildRows()
        {
            if (!lockstep.IsContinuationFromPrevFrame && currentMode == ObjectsPageMode.Creating)
                ExitCreatingMode(); // Creating relies on an active row.
            rowsList.RebuildRows();
        }

        #endregion

        #region Favorite

        public void OnFavoriteValueChanged(ObjectsRow row)
        {
            bool isFavorite = row.favoriteToggle.isOn;
            if (isFavorite)
                objectsFavoritesManager.SendAddFavoriteIA(localPlayer, row.entityPrototype);
            else
                objectsFavoritesManager.SendRemoveFavoriteIA(localPlayer, row.entityPrototype);
            // Latency hiding.
            row.isFavorite = isFavorite;
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        [ObjectsFavoritesEvent(ObjectsFavoritesEventType.OnObjectFavoriteAdded)]
        public void OnObjectFavoriteAdded() => OnObjectFavoriteChanged(true);

        [ObjectsFavoritesEvent(ObjectsFavoritesEventType.OnObjectFavoriteRemoved)]
        public void OnObjectFavoriteRemoved() => OnObjectFavoriteChanged(false);

        private void OnObjectFavoriteChanged(bool isFavorite)
        {
            // No need for an isInitialized check, this can only trigger through an input action, not any GS safe context.
            if (!objectsFavoritesManager.PlayerForEvent.core.isLocal)
                return;
            if (!TryGetRow(objectsFavoritesManager.EntityPrototypeForEvent.Id, out ObjectsRow row))
                return;
            row.isFavorite = isFavorite;
            row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
            rowsList.PotentiallySortChangedFavoriteRow(row);
        }

        private void UpdateAllFavorites()
        {
            ObjectsRow[] rows = rowsList.Rows;
            int rowsCount = rowsList.RowsCount;
            bool anyChanged = false;
            for (int i = 0; i < rowsCount; i++)
            {
                ObjectsRow row = rows[i];
                bool isFavorite = localPlayer.favoriteObjectIdsLut.ContainsKey(row.entityPrototype.Id);
                if (row.isFavorite == isFavorite)
                    continue;
                row.isFavorite = isFavorite;
                row.favoriteToggle.SetIsOnWithoutNotify(isFavorite);
                anyChanged = true;
            }
            if (anyChanged)
                rowsList.SortAllRows();
        }

        #endregion

        #region Row Highlight

        private void ClearActiveRow()
        {
            if (activeRow == null)
                return;
            activeRow.highlightToggle.SetIsOnWithoutNotify(false);
            activeRow = null;
        }

        private void SetActiveRow(ObjectsRow row)
        {
            if (activeRow == row)
                return;
            ClearActiveRow();
            activeRow = row;
        }

        public void OnHighlightToggleValueChanged(ObjectsRow row)
        {
            if (row.highlightToggle.isOn)
                EnterCreatingMode(row);
            else if (row == activeRow)
                EnterIdleMode();
        }

        #endregion

        #region Modes

        private void EnterIdleMode()
        {
            switch (currentMode)
            {
                case ObjectsPageMode.Creating:
                    ExitCreatingMode();
                    break;
                case ObjectsPageMode.Editing:
                    ExitEditingMode();
                    break;
                case ObjectsPageMode.Deleting:
                    ExitDeletingMode();
                    break;
            }
        }

        private void UpdateCurrentMode()
        {
            switch (currentMode)
            {
                case ObjectsPageMode.Creating:
                    UpdateCreatingMode();
                    break;
                case ObjectsPageMode.Editing:
                    UpdateEditingMode();
                    break;
                case ObjectsPageMode.Deleting:
                    UpdateDeletingMode();
                    break;
            }
        }

        private void HandleInputUseForCurrentMode()
        {
            switch (currentMode)
            {
                case ObjectsPageMode.Creating:
                    HandleInputUseForCreatingMode();
                    break;
                case ObjectsPageMode.Editing:
                    HandleInputUseForEditingMode();
                    break;
                case ObjectsPageMode.Deleting:
                    HandleInputUseForDeletingMode();
                    break;
            }
        }

        private void DetermineInteractingHand(string callbackEventName, object callbackCustomData)
        {
            if (!isInVR)
            {
                determinedHand = VRCPlayerApi.TrackingDataType.Head;
                determinedHandCallbackCustomData = callbackCustomData;
                SendCustomEvent(callbackEventName);
                return;
            }

            MenuPositionType menuPosition = menuInputHandler.MenuPosition;
            // Explicitly checking for these 2 types in case more menu position types get added in the future.
            if (menuPosition != MenuPositionType.LeftHand && menuPosition != MenuPositionType.RightHand)
            {
                inputManager.DetermineHandUsedForClick(this, nameof(OnInteractingHandDetermined), new object[]
                {
                    callbackEventName,
                    callbackCustomData,
                });
                return;
            }

            determinedHand = menuPosition == MenuPositionType.LeftHand
                ? VRCPlayerApi.TrackingDataType.LeftHand
                : VRCPlayerApi.TrackingDataType.RightHand;
            determinedHandCallbackCustomData = callbackCustomData;
            SendCustomEvent(callbackEventName);
        }

        public void OnInteractingHandDetermined()
        {
            determinedHand = inputManager.DeterminedHand;
            object[] callbackCustomData = (object[])inputManager.CallbackCustomData;
            determinedHandCallbackCustomData = callbackCustomData[1];
            SendCustomEvent((string)callbackCustomData[0]);
        }

        private bool RaycastAndShowLaser(out Vector3 forward, out RaycastHit hit)
        {
            var hand = localPlayerApi.GetTrackingData(activeHand);
            Vector3 position = hand.position;
            Quaternion rotation = hand.rotation;
            if (isInVR)
            {
                position += rotation * interactables.GetAnchorOffsetVector(activeHand);
                rotation *= interactables.GetRotationNormalization(activeHand);
            }

            forward = rotation * Vector3.forward;

            bool didHit = Physics.Raycast(
                position,
                forward,
                out hit,
                maxRaycastDistance,
                objectsCollisionLayers,
                QueryTriggerInteraction.Ignore);

            float laserDistance = didHit ? hit.distance : maxRaycastDistance;

            if (!didHit || !isInVR) // No laser in desktop.
                laserPointerRootGo.SetActive(false);
            else
            {
                laserPointerRoot.SetPositionAndRotation(position, rotation);
                laserPointerRoot.localScale = new Vector3(1f, 1f, laserDistance);
                laserPointerRootGo.SetActive(true);
            }

            return didHit;
        }

        #region Popups

        private void ShowPopup(RectTransform popup)
        {
            menuManager.ShowPopupAtItsAnchor(popup, this, nameof(OnPopupClosed));
            activePopup = popup;
        }

        private void CloseActivePopup()
        {
            if (activePopup == null)
                return;
            menuManager.ClosePopup(activePopup, doCallback: false);
            ReturnActivePopup();
        }

        public void OnPopupClosed()
        {
            ReturnActivePopup();
            EnterIdleMode();
        }

        private void ReturnActivePopup()
        {
            activePopup.SetParent(popupsParent, worldPositionStays: false);
            activePopup = null;
        }

        #endregion

        #region Creating

        private void EnterCreatingMode(ObjectsRow row)
        {
            DetermineInteractingHand(nameof(OnCreatingActiveHandDetermined), callbackCustomData: row);
        }

        public void OnCreatingActiveHandDetermined()
        {
            EnterCreatingMode((ObjectsRow)determinedHandCallbackCustomData, determinedHand);
        }

        private void EnterCreatingMode(ObjectsRow row, VRCPlayerApi.TrackingDataType activeHand)
        {
            EnterIdleMode();
            currentMode = ObjectsPageMode.Creating;
            SetActiveRow(row);
            SetActiveHand(activeHand);
            creatingHeaderLabel.text = string.Format(creatingHeaderLabelFormat, row.entityPrototype.DisplayName);
            currentCreationPreviewGo = objectsPageManager.GetCreationPreviewInstanceForPrototype(row.entityPrototype);
            updateManager.Register(this);
            ShowPopup(creatingPopup);
            UpdateCreatingMode();
        }

        private void ExitCreatingMode()
        {
            currentMode = ObjectsPageMode.Idle;
            ClearActiveRow();
            CloseActivePopup();
            updateManager.Deregister(this);
            laserPointerRootGo.SetActive(false);
            currentCreationPreviewGo.SetActive(false);
            currentCreationPreviewGo = null;
        }

        private void UpdateCreatingMode()
        {
            if (menuManager.PointerIsOnMenu)
            {
                laserPointerRootGo.SetActive(false);
                currentCreationPreviewGo.SetActive(false);
                return;
            }

            if (!RaycastAndShowLaser(out Vector3 forward, out RaycastHit hit))
            {
                currentCreationPreviewGo.SetActive(false);
                return;
            }

            if (referenceWorldForwardToggle.isOn)
                forward = Vector3.forward;

            Vector3 up = alignmentTargetSurfaceNormalToggle.isOn ? hit.normal : Vector3.up;
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, up);
            if (projectedForward == Vector3.zero)
                projectedForward = Quaternion.LookRotation(forward: up) * Vector3.down; // Some direction perpendicular to up.
            else
                projectedForward = projectedForward.normalized;

            Quaternion rotation = Quaternion.LookRotation(projectedForward, up) * facingOptions.OffsetRotation;

            currentCreationPreviewGo.transform.SetPositionAndRotation(hit.point, rotation);
            currentCreationPreviewGo.SetActive(true);
        }

        private void HandleInputUseForCreatingMode()
        {
            if (!currentCreationPreviewGo.activeSelf)
                return;

            EntityData entityData = entitySystem.SendCreateEntityIA(
                activeRow.entityPrototype.Id,
                currentCreationPreviewGo.transform.position,
                currentCreationPreviewGo.transform.rotation);

            facingOptions.OnObjectCreated();

            if (uponPlaceClosePopupToggle.isOn)
            {
                ExitCreatingMode();
                return;
            }

            if (uponPlaceSwitchToEditToggle.isOn)
            {
                EnterEditingMode(activeHand, entityData);
                return;
            }

            // No point in calling UpdateCreatingMode when placing more, creating entities is not instant.
        }

        #endregion

        #region Pointed At Object

        private bool TryGetPointedAtObjectAndShowLaser(out bool pointerTargetChanged, out Entity entity)
        {
            if (!RaycastAndShowLaser(out Vector3 discard, out RaycastHit hit)
                || hit.transform == null) // null for VRChat internals.
            {
                pointerTargetChanged = lastPointedAtTransform != null;
                lastPointedAtTransform = null;
                entity = null;
                return false;
            }

            Transform hitTransform = hit.transform;
            if (lastPointedAtTransform == hitTransform)
            {
                pointerTargetChanged = false;
                entity = null;
                return false;
            }
            lastPointedAtTransform = hitTransform;
            pointerTargetChanged = true;

            entity = hitTransform.GetComponentInParent<Entity>();
            if (entity != null && objectsPageManager.ObjectPrototypeNamesLut.ContainsKey(entity.prototype.PrototypeName))
                return true;

            entity = null;
            return false;
        }

        private void SetPointedAtObject(ObjectEntityExtension pointedAtObject)
        {
            if (this.pointedAtObject == pointedAtObject)
                return;
            if (this.pointedAtObject != null)
                this.pointedAtObject.HideHighlight();
            this.pointedAtObject = pointedAtObject;
            if (this.pointedAtObject != null)
                this.pointedAtObject.ShowHighlight();
        }

        private void UpdatePointedAtObjectAndLaser()
        {
            if (TryGetPointedAtObjectAndShowLaser(out bool pointerTargetChanged, out Entity entity))
                SetPointedAtObject(entity.GetExtension<ObjectEntityExtension>(nameof(ObjectEntityExtensionData)));
            else if (pointerTargetChanged)
                SetPointedAtObject(null);
        }

        private void StopPointingAtObjects()
        {
            laserPointerRootGo.SetActive(false);
            lastPointedAtTransform = null;
            SetPointedAtObject(null);
        }

        #endregion

        #region Editing

        public void OnEditExistingClick() => EnterEditingMode();

        public void OnDeselectCurrentEditingClick() => SetEditingEntityData(null);

        public void OnDuplicateEditingClick() => DuplicateEditingEntity();

        private void EnterEditingMode()
        {
            DetermineInteractingHand(nameof(OnEditingActiveHandDetermined), callbackCustomData: null);
        }

        public void OnEditingActiveHandDetermined()
        {
            EnterEditingMode(determinedHand);
        }

        private void EnterEditingMode(VRCPlayerApi.TrackingDataType activeHand, EntityData editingEntityData = null)
        {
            EnterIdleMode();
            currentMode = ObjectsPageMode.Editing;
            SetActiveHand(activeHand);
            updateManager.Register(this);
            SetEditingEntityData(editingEntityData);
            ShowPopup(editingPopup);
            UpdateEditingMode();
        }

        private void ExitEditingMode()
        {
            currentMode = ObjectsPageMode.Idle;
            CloseActivePopup();
            updateManager.Deregister(this);
            StopPointingAtObjects();
            SetEditingEntityData(null);
        }

        private void SetEditingEntityData(EntityData editingEntityData)
        {
            if (this.editingEntityData == editingEntityData)
                return;
            this.editingEntityData = editingEntityData;
            bool hasEditingEntityData = editingEntityData != null;

            if (hasEditingEntityData)
                StopPointingAtObjects();
            UpdateEntityTransformGizmo();

            string objectName = hasEditingEntityData ? editingEntityData.entityPrototype.DisplayName : "";
            editingHeaderLabel.text = string.Format(editingHeaderLabelFormat, objectName).Trim();

            editingInfoWhileDeselected.SetActive(!hasEditingEntityData);
            editingInfoWhileSelected.SetActive(hasEditingEntityData);
            editingDeselectCurrentButton.interactable = hasEditingEntityData;
            editingDeselectCurrentButtonLabel.interactable = hasEditingEntityData;
            editingDuplicateButton.interactable = hasEditingEntityData;
            editingDuplicateButtonLabel.interactable = hasEditingEntityData;
        }

        private void UpdateEntityTransformGizmo()
        {
            waitingForEditingEntityToGetCreated = false;

            if (editingEntityData == null)
            {
                entityTransformGizmo.StopTracking();
                return;
            }

            Entity entity = editingEntityData.entity;
            if (entity == null)
            {
                entityTransformGizmo.StopTracking();
                waitingForEditingEntityToGetCreated = true;
                return;
            }

            entityTransformGizmo.SetTrackedEntity(entity, activeHand);
        }

        private void UpdateEditingMode()
        {
            if (editingEntityData != null)
            {
                if (!editingEntityData.CheckLiveliness() || editingEntityData.entityIsDestroyed)
                    SetEditingEntityData(null);
                else if (waitingForEditingEntityToGetCreated && editingEntityData.entity != null)
                    UpdateEntityTransformGizmo();
                return;
            }

            if (menuManager.PointerIsOnMenu)
            {
                StopPointingAtObjects();
                return;
            }

            UpdatePointedAtObjectAndLaser();
        }

        private void HandleInputUseForEditingMode()
        {
            if (editingEntityData != null || pointedAtObject == null)
                return;
            SetEditingEntityData(pointedAtObject.entityData);
            UpdateEditingMode();
        }

        private void DuplicateEditingEntity()
        {
            if (editingEntityData == null)
                return;
            Vector3 position;
            Quaternion rotation;
            Vector3 scale;
            if (editingEntityData.entity != null)
            {
                Transform entityTransform = editingEntityData.entity.transform;
                position = entityTransform.position;
                rotation = entityTransform.rotation;
                scale = entityTransform.localScale;
            }
            else
            {
                position = editingEntityData.position;
                rotation = editingEntityData.rotation;
                scale = editingEntityData.scale;
            }
            EntityData duplicatedEntityData = SendCreateScaledObjectIA(editingEntityData.entityPrototype, position, rotation, scale);
            SetEditingEntityData(duplicatedEntityData);
        }

        private EntityData SendCreateScaledObjectIA(EntityPrototype prototype, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!lockstep.IsInitialized)
                return null;
            lockstep.WriteVector3(scale);
            EntityData entityData = entitySystem.SendCustomCreateEntityIA(onCreateScaledObjectIAId, prototype.Id, position, rotation);

            InitializeEntity(entityData, scale); // Latency hiding.
            return entityData;
        }

        [HideInInspector][SerializeField] private uint onCreateScaledObjectIAId;
        [LockstepInputAction(nameof(onCreateScaledObjectIAId))]
        public void OnCreateScaledObjectIA()
        {
            Vector3 scale = lockstep.ReadVector3();
            EntityData entityData = entitySystem.ReadEntityInCustomCreateEntityIA(onEntityCreatedGetsRaisedLater: true);

            if (lockstep.SendingPlayerId != localPlayerId) // The sending local player already performed this initialization.
                InitializeEntity(entityData, scale);

            entitySystem.RaiseOnEntityCreatedInCustomCreateEntityIA(entityData);
        }

        private void InitializeEntity(EntityData entityData, Vector3 scale)
        {
            entityData.scale = scale;
            // By the time this runs the entity for this entityData is guaranteed to not exist yet.
            // Therefore there is no need for any additional logic here, when the entity gets created it will
            // use the entityData, including the values that got populated above.
        }

        #endregion

        #region Deleting

        public void OnDeleteExistingClick() => EnterDeletingMode();

        private void EnterDeletingMode()
        {
            DetermineInteractingHand(nameof(OnDeletingActiveHandDetermined), callbackCustomData: null);
        }

        public void OnDeletingActiveHandDetermined()
        {
            EnterDeletingMode(determinedHand);
        }

        private void EnterDeletingMode(VRCPlayerApi.TrackingDataType activeHand)
        {
            EnterIdleMode();
            currentMode = ObjectsPageMode.Deleting;
            SetActiveHand(activeHand);
            updateManager.Register(this);
            ShowPopup(deletingPopup);
            UpdateDeletingMode();
        }

        private void ExitDeletingMode()
        {
            currentMode = ObjectsPageMode.Idle;
            CloseActivePopup();
            updateManager.Deregister(this);
            StopPointingAtObjects();
        }

        private void UpdateDeletingMode()
        {
            if (menuManager.PointerIsOnMenu)
            {
                StopPointingAtObjects();
                return;
            }

            UpdatePointedAtObjectAndLaser();
        }

        private void HandleInputUseForDeletingMode()
        {
            if (pointedAtObject == null)
                return;
            entitySystem.SendDestroyEntityIA(pointedAtObject.entityData);
            UpdateDeletingMode();
        }

        #endregion

        #endregion
    }
}
