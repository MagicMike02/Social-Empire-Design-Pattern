using UnityEngine;
using Script2.Common;
using Script2.GridSystem;
using VContainer;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Gestisce il processo di piazzamento degli edifici con preview visiva e validazione in tempo reale.
    /// REFACTORED: Usa Dependency Injection (VContainer) invece di Singleton pattern.
    /// </summary>
    public sealed class BuildingPlacer : MonoBehaviour
    {
        #region Dependencies (Injected)

        private BuildingManager _manager;
        private Camera _camera;
        private GenericPreviewSystem _previewSystem;
        private BuildingEventBus _eventBus;
        private ZoneManager _zoneManager;

        [Inject]
        public void Construct(
            BuildingManager manager,
            Camera mainCamera, 
            GenericPreviewSystem previewSystem,
            BuildingEventBus eventBus,
            ZoneManager zoneManager)
        {
            _manager = manager;
            _camera = mainCamera;
            _previewSystem = previewSystem;
            _eventBus = eventBus;
            _zoneManager = zoneManager;
        }

        #endregion

        [Header("State (Debug Only)")] [SerializeField]
        private BuildingConfigSO _selectedConfig;

        [SerializeField] private bool _isPlacing;
        [SerializeField] private Vector3Int _currentCell;

        private Vector3Int _lastCell = Vector3Int.one * -1000;
        private bool _lastValidState = true;


        #region Properties

        /// <summary>
        /// Indica se il placer è attualmente in modalità placement.
        /// </summary>
        public bool IsPlacing => _isPlacing;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_isPlacing) return;
            UpdatePlacementPreview();
        }

        private void OnDestroy()
        {
            // Cleanup: nascondi preview se ancora presente
            if (_previewSystem != null)
            {
                _previewSystem.HidePreview();
            }

            // Rimuovi preview griglia tile
            CleanupGridPreview();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inizia il processo di piazzamento per una configurazione di edificio specificata.
        /// </summary>
        public void StartPlacing(BuildingConfigSO config)
        {
            if (config == null)
            {
                Debug.LogWarning("[BuildingPlacer] Impossibile avviare placement: configurazione null.");
                return;
            }

            if (config.Prefab == null)
            {
                Debug.LogWarning($"[BuildingPlacer] Impossibile creare preview: prefab mancante per '{config.name}'.");
                return;
            }

            // Se già in placement, annulla quello precedente
            if (_isPlacing)
            {
                CancelPlacement();
            }

            _selectedConfig = config;
            _isPlacing = true;
            _lastCell = Vector3Int.one * -1000; // Reset cache
            _lastValidState = true;
        }

        /// <summary>
        /// Conferma il piazzamento dell'edificio nella posizione corrente se valida.
        /// </summary>
        public void ConfirmPlacement()
        {
            if (!_isPlacing || _selectedConfig == null)
            {
                Debug.LogWarning("[BuildingPlacer] Impossibile confermare: non in modalità placement.");
                return;
            }

            // Validazione finale
            if (!CanPlaceBuilding(_selectedConfig, _currentCell))
            {
                Debug.Log("[BuildingPlacer] ✗ Piazzamento fallito: posizione non valida o risorse insufficienti.");
                return;
            }

            // Crea edificio reale
            var worldPos = _manager.Grid.CellToWorld(_currentCell);
            var building = _manager.Factory.CreateBuilding(_selectedConfig, worldPos, _manager.Root);

            if (building == null)
            {
                Debug.LogError("[BuildingPlacer] Creazione building fallita.");
                return;
            }

            // Spendi risorse e occupa celle
            _manager.Economy?.SpendResources(_selectedConfig.ToDictionary());
            _manager.Grid.OccupyCells(_currentCell, _selectedConfig.Width, _selectedConfig.Height, building);

            Debug.Log($"[BuildingPlacer] Edificio piazzato: {_selectedConfig.name} alla posizione {worldPos}");

            // Notifica evento tramite EventBus (no static events!)
            _eventBus.RaiseBuildingPlaced(building);

            // Termina placement
            CancelPlacement();
        }

        /// <summary>
        /// Annulla il placement corrente e pulisce la preview.
        /// </summary>
        public void CancelPlacement()
        {
            if (!_isPlacing)
            {
                return;
            }

            // Nascondi preview edificio
            if (_previewSystem != null)
            {
                _previewSystem.HidePreview();
            }

            // Pulisci preview griglia tile
            CleanupGridPreview();

            _isPlacing = false;
            _selectedConfig = null;

            Debug.Log("[BuildingPlacer] ✓ Placement annullato");
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

            // Controllo 3: Nessuna risorsa sulle celle
            bool noResourcesOnCells = AreCellsFreeOfResources(originCell, config.Width, config.Height);

            return cellsFree && canAfford && noResourcesOnCells;
        }

        /// <summary>
        /// Verifica se le celle non contengono risorse.
        /// </summary>
        private bool AreCellsFreeOfResources(Vector3Int originCell, int width, int height)
        {
            if (_zoneManager == null) return true;

            // Verifica se qualche cella è occupata da una risorsa
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var checkCell = new Vector2Int(originCell.x + dx, originCell.y + dy);

                    // Controlla se la cella è occupata in occupiedTiles (risorse/decorazioni)
                    if (_zoneManager.occupiedTiles.ContainsKey(checkCell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}