using Script.BuildingSystem.Commands;
using Script.BuildingSystem.States;
using Script.Common;
using Script.Core.Commands;
using Script.Core.Events;
using Script.GridSystem;
using UnityEngine;
using VContainer;

namespace Script.BuildingSystem
{
    public sealed class BuildingPlacer : MonoBehaviour
    {
        #region Dependencies (Injected)

        private BuildingManager _manager;
        private Camera _camera;
        private GenericPreviewSystem _previewSystem;
        private ZoneManager _zoneManager;
        private CommandHistory _commandHistory;

        [Inject]
        public void Construct(
            BuildingManager manager,
            Camera mainCamera, 
            GenericPreviewSystem previewSystem,
            ZoneManager zoneManager,
            CommandHistory commandHistory)
        {
            _manager = manager;
            _camera = mainCamera;
            _previewSystem = previewSystem;
            _zoneManager = zoneManager;
            _commandHistory = commandHistory;
        }

        #endregion

        #region State Machine

        private IPlacementState _currentState;
        private IPlacementState _idleState;

        #endregion

        #region Private Fields (State-managed)

        // NOTE: Questi campi sono gestiti dagli Stati, non modificare direttamente
        [Header("State (Debug Only)")] [SerializeField]
        private BuildingConfigSO _selectedConfig;

        [SerializeField] private Vector3Int _currentCell;

        private Vector3Int _lastCell = Vector3Int.one * -1000;
        private bool _lastValidState = true;

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
            // Inizializza lo stato di inattività e lo imposta come stato corrente.
            _idleState = new IdlePlacementState(this);
            _currentState = _idleState;
            _currentState.OnEnter();
        }

        private void Update()
        {
            // Delega logica Update allo stato corrente
            _currentState?.OnUpdate();
        }

        private void OnDestroy()
        {
            // Exit stato corrente
            _currentState?.OnExit();
            
            // Cleanup: nascondi preview se ancora presente
            if (_previewSystem != null)
            {
                _previewSystem.HidePreview();
            }

            // Rimuovi preview griglia tile
            CleanupGridPreview();
        }

        #endregion

        #region Public API (Event-Driven, chiamati da Input Handlers)

        /// <summary>
        /// Seleziona edificio da piazzare.
        /// Chiamato da: UI button, keyboard shortcut, etc.
        /// Trigger: OnBuildingSelected event nello stato corrente.
        /// </summary>
        public void SelectBuilding(BuildingConfigSO config)
        {
            _currentState?.OnBuildingSelected(config);
        }

        /// <summary>
        /// Conferma piazzamento edificio.
        /// Chiamato da: Mouse left click, UI confirm button, etc.
        /// Trigger: OnPlacementConfirmed event nello stato corrente.
        /// </summary>
        public void ConfirmPlacement()
        {
            _currentState?.OnPlacementConfirmed();
        }

        /// <summary>
        /// Cancella piazzamento edificio.
        /// Chiamato da: Mouse right click, ESC key, UI cancel button, etc.
        /// Trigger: OnPlacementCancelled event nello stato corrente.
        /// </summary>
        public void CancelPlacement()
        {
            _currentState?.OnPlacementCancelled();
        }

        #endregion

        #region FSM Transition

        /// <summary>
        /// Transizione a nuovo stato FSM.
        /// Chiamato dagli stati stessi per navigare tra stati.
        /// </summary>
        public void TransitionTo(IPlacementState newState)
        {
            if (_currentState != null)
            {
                _currentState.OnExit();
            }

            _currentState = newState;

            if (_currentState != null)
            {
                _currentState.OnEnter();
            }

            #if UNITY_EDITOR
            Debug.Log($"[BuildingPlacer] FSM Transition → {_currentState?.StateName ?? "null"}");
            #endif
        }

        #endregion

        #region Public Methods (per Stati FSM)

        /// <summary>
        /// Imposta configurazione edificio selezionato (chiamato da PreviewingState).
        /// </summary>
        public void SetSelectedConfig(BuildingConfigSO config)
        {
            // Puliamo le preview visive sulla vecchia griglia e prefab prima di assegnare la nuova config
            CleanupGridPreview();
            
            if (_previewSystem != null)
            {
                _previewSystem.HidePreview();
            }

            _selectedConfig = config;
            _lastCell = Vector3Int.one * -1000; // Reset cache (forza il redraw)
            _lastValidState = true;
        }

        /// <summary>
        /// Abilita/disabilita modalità preview (chiamato dagli Stati).
        /// </summary>
        public void EnablePreviewMode(bool isEnabled)
        {
            // Logica gestita da UpdatePlacementPreviewInternal
            // Questo metodo esiste per compatibilità Stati
        }

        /// <summary>
        /// Pulisce preview (chiamato da IdleState, ConfirmingState).
        /// </summary>
        public void ClearPreview()
        {
            if (_previewSystem != null)
            {
                _previewSystem.HidePreview();
            }
            
            CleanupGridPreview();
            
            _selectedConfig = null;
            _lastCell = Vector3Int.one * -1000;
        }

        /// <summary>
        /// Verifica se può piazzare edificio alla posizione corrente (chiamato da PreviewingState).
        /// </summary>
        public bool CanPlaceAtCurrentPosition()
        {
            if (_selectedConfig == null) return false;
            return _lastValidState; // Cached da UpdatePlacementPreviewInternal
        }

        /// <summary>
        /// Esegue placement via Command Pattern (chiamato da ConfirmingState).
        /// </summary>
        public bool ExecutePlacementCommand()
        {
            if (_selectedConfig == null)
            {
                Debug.LogWarning("[BuildingPlacer] ExecutePlacementCommand: nessun edificio selezionato");
                return false;
            }

            // Crea comando PlaceBuilding
            var command = new PlaceBuildingCommand(
                _manager,
                _manager.Grid,
                _manager.Economy,
                _selectedConfig,
                _currentCell
            );

            // Esegui comando tramite CommandHistory (abilita Undo)
            bool success = _commandHistory.ExecuteCommand(command);

            if (success)
            {
                // Pubblica evento GlobalEventBus
                GlobalEventBus.Publish(new BuildingPlacedEvent(
                    null, // Building non accessibile da command (privacy)
                    _currentCell,
                    _selectedConfig.name
                ));

                Debug.Log($"[BuildingPlacer] ✓ Edificio piazzato: {_selectedConfig.name} at {_currentCell}");
            }

            return success;
        }

        /// <summary>
        /// Update posizione preview (chiamato da PreviewingState.OnUpdate).
        /// </summary>
        public void UpdatePlacementPreviewInternal()
        {
            if (_selectedConfig == null) return;
            
            UpdatePlacementPreview();
        }

        #endregion

        #region Private Methods

        private void CleanupGridPreview()
        {
            if (_manager?.Grid != null && _selectedConfig != null)
            {
                _manager.Grid.SetCellsPreview(_currentCell, 0, 0, false);
            }
        }

        private void UpdatePlacementPreview()
        {
            if (_camera == null || _manager?.Grid == null || _selectedConfig == null || _previewSystem == null)
            {
                return;
            }

            // STEP 1: Conversione mouse → world
            var mousePos = Input.mousePosition;
            var worldPos = _camera.ScreenToWorldPoint(mousePos);
            worldPos.z = 1f;

            // STEP 2: Converti a cella della griglia
            if (!_manager.Grid.TryWorldToCell(worldPos, out var cell))
            {
                return;
            }

            // STEP 3: Se cella non è cambiata, non aggiornare (ottimizzazione)
            if (cell == _lastCell)
            {
                return;
            }

            _currentCell = cell;

            // Calcola posizione world snappata per il building finale
            var snapPos = _manager.Grid.CellToWorld(cell);


            // STEP 5: Validazione (il building può essere piazzato qui?)
            bool isValid = CanPlaceBuilding(_selectedConfig, cell);


            // STEP 6: Aggiorna il preview visivo del building con posizione snappata
            bool updated = _previewSystem.UpdatePreviewIfCellChanged(cell, snapPos, isValid);

            // Se prima volta o cella cambiata, mostra preview
            if (_lastCell.x == -1000 || updated)
            {
                _previewSystem.ShowPreview(_selectedConfig.Prefab, snapPos, isValid);
            }

            // Aggiorna preview griglia tile
            if (updated || _lastValidState != isValid)
            {
                _manager.Grid.SetCellsPreview(cell, _selectedConfig.Width, _selectedConfig.Height, isValid);
                _lastValidState = isValid;
            }

            _lastCell = cell;
        }

        /// <summary>
        /// Validazione consolidata del piazzamento edifici.
        /// Verifica: 1) Celle libere, 2) Risorse sufficienti, 3) Nessuna risorsa sulle celle
        /// </summary>
        private bool CanPlaceBuilding(BuildingConfigSO config, Vector3Int originCell)
        {
            if (_manager.Grid == null || config == null)
            {
                return false;
            }

            // Controllo 1: Celle libere (non occupate da altri edifici)
            bool cellsFree = _manager.Grid.AreCellsFree(originCell, config.Width, config.Height);

            // Controllo 2: Risorse sufficienti
            bool canAfford = _manager.Economy == null || _manager.Economy.CanAfford(config.ToDictionary());

            return cellsFree && canAfford;
        }

        #endregion
    }
}

