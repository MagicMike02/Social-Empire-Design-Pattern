using Script.BuildingSystem.Commands;
using Script.BuildingSystem.States;
using Script.Common;
using Script.Core.Commands;
using Script.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Orchestratore FSM per il piazzamento edifici.
    /// Responsabilità: gestione stati FSM, esecuzione Command Pattern, Public API.
    /// Preview visiva delegata a <see cref="BuildingPreviewController"/>.
    /// Validazione delegata a <see cref="BuildingValidationService"/>.
    /// </summary>
    public sealed class BuildingPlacer : MonoBehaviour
    {
        #region Dependencies (Injected)

        private BuildingManager _manager;
        private CommandHistory _commandHistory;
        private BuildingValidationService _validationService;
        private Camera _mainCamera;
        private GenericPreviewSystem _previewSystem;

        [Inject]
        public void Construct(
            BuildingManager manager,
            CommandHistory commandHistory,
            BuildingValidationService validationService,
            Camera mainCamera,
            GenericPreviewSystem previewSystem)
        {
            try
            {
                _manager = manager;
                _commandHistory = commandHistory;
                _validationService = validationService;
                _mainCamera = mainCamera;
                _previewSystem = previewSystem;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[BuildingPlacer] Errore durante Construct: {ex.Message}");
#endif
            }
        }

        #endregion

        #region State Machine

        private IPlacementState _currentState;
        private readonly BuildingPlacementStateTracker _placementState = new();
        private BuildingPreviewController _previewController;

        #endregion

        #region Private Fields

        [Header("Input Actions")]
        [SerializeField] private InputActionReference _pointAction;

        #endregion

        #region Properties

        /// <summary>
        /// Indica se il placer è attualmente in modalità placement.
        /// </summary>
        public bool IsPlacing => _currentState != null && _currentState.StateName != "Idle";

        /// <summary>
        /// Stato corrente FSM (per debugging).
        /// </summary>
        public string CurrentStateName => _currentState?.StateName ?? "Uninitialized";

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (!ValidateDependencies())
            {
                return;
            }

            // Costruisce il controller di preview con le dipendenze risolte.
            _previewController = new BuildingPreviewController(
                _mainCamera,
                _previewSystem,
                _manager,
                _validationService,
                _placementState,
                _pointAction);

            // Inizializza lo stato di inattività e lo imposta come stato corrente.
            _currentState = new IdlePlacementState(this);
            _currentState.OnEnter();
        }

        private void Update()
        {
            _currentState?.OnUpdate();
        }

        private void OnEnable()
        {
            _pointAction?.action?.Enable();
        }

        private void OnDisable()
        {
            _pointAction?.action?.Disable();
        }

        private void OnDestroy()
        {
            _currentState?.OnExit();
            _previewController?.ClearPreview();
        }

        #endregion

        #region Public API (Event-Driven, chiamati da Input Handlers)

        /// <summary>
        /// Seleziona edificio da piazzare.
        /// Chiamato da: UI button, keyboard shortcut, etc.
        /// </summary>
        public void SelectBuilding(BuildingConfigSO config)
        {
            _currentState?.OnBuildingSelected(config);
        }

        /// <summary>
        /// Conferma piazzamento edificio.
        /// </summary>
        public void ConfirmPlacement()
        {
            _currentState?.OnPlacementConfirmed();
        }

        /// <summary>
        /// Cancella piazzamento edificio.
        /// </summary>
        public void CancelPlacement()
        {
            _currentState?.OnPlacementCancelled();
        }

        #endregion

        #region FSM Transition

        /// <summary>
        /// Transizione a nuovo stato FSM.
        /// </summary>
        public void TransitionTo(IPlacementState newState)
        {
            _currentState?.OnExit();
            _currentState = newState;
            _currentState?.OnEnter();

#if UNITY_EDITOR
            Debug.Log($"[BuildingPlacer] FSM Transition → {_currentState?.StateName ?? "null"}");
#endif
        }

        private bool ValidateDependencies()
        {
            if (_manager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BuildingPlacer] BuildingManager non iniettato!");
#endif
                return false;
            }

            if (_commandHistory == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BuildingPlacer] CommandHistory non iniettato!");
#endif
                return false;
            }

            if (_validationService == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BuildingPlacer] BuildingValidationService non iniettato!");
#endif
                return false;
            }

            if (_mainCamera == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BuildingPlacer] Camera non iniettata!");
#endif
                return false;
            }

            if (_previewSystem == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BuildingPlacer] GenericPreviewSystem non iniettato!");
#endif
                return false;
            }

            return true;
        }

        #endregion

        #region Public Methods (per Stati FSM)

        /// <summary>
        /// Imposta configurazione edificio selezionato (chiamato da PreviewingState).
        /// Delega cleanup preview al controller.
        /// </summary>
        public void SetSelectedConfig(BuildingConfigSO config)
        {
            _previewController?.CleanupBeforeNewSelection();
            _placementState.Select(config);
        }

        /// <summary>
        /// Pulisce preview (chiamato da IdleState, ConfirmingState).
        /// </summary>
        public void ClearPreview()
        {
            _previewController?.ClearPreview();
        }

        /// <summary>
        /// Verifica se può piazzare edificio alla posizione corrente (chiamato da PreviewingState).
        /// </summary>
        public bool CanPlaceAtCurrentPosition()
        {
            return _placementState.CanPlaceAtCurrentPosition();
        }

        /// <summary>
        /// Esegue placement via Command Pattern (chiamato da ConfirmingState).
        /// </summary>
        public bool ExecutePlacementCommand()
        {
            var selectedConfig = _placementState.SelectedConfig;
            if (selectedConfig == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[BuildingPlacer] ExecutePlacementCommand: nessun edificio selezionato");
#endif
                return false;
            }

            var command = new PlaceBuildingCommand(
                _manager,
                _manager.Grid,
                _manager.Economy,
                selectedConfig,
                _placementState.CurrentCell
            );

            bool success = _commandHistory.ExecuteCommand(command);

            if (success)
            {
                GlobalEventBus.Publish(new BuildingPlacedEvent(
                    null,
                    _placementState.CurrentCell,
                    selectedConfig.name
                ));

#if UNITY_EDITOR
                Debug.Log($"[BuildingPlacer] ✓ Edificio piazzato: {selectedConfig.name} at {_placementState.CurrentCell}");
#endif
            }

            return success;
        }

        /// <summary>
        /// Update posizione preview (chiamato da PreviewingState.OnUpdate).
        /// Delega al BuildingPreviewController.
        /// </summary>
        public void UpdatePlacementPreviewInternal()
        {
            if (_placementState.SelectedConfig == null) return;

            _previewController?.UpdatePlacementPreview();
        }

        #endregion
    }
}
