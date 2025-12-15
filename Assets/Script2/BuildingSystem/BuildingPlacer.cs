﻿using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Gestisce il processo di piazzamento degli edifici con preview visiva e validazione in tempo reale.
    /// Utilizza il pattern State per gestire le fasi: Idle → Placing → Confirming.
    /// </summary>
    public sealed class BuildingPlacer : MonoBehaviour
    {
        #region Fields
        
        [Header("Dependencies")]
        [SerializeField] private BuildingManager _manager;
        [SerializeField] private Camera _camera;

        [Header("State (Debug Only)")]
        [SerializeField] private BuildingConfigSO _selectedConfig;
        [SerializeField] private BuildingGhost _ghostInstance;
        [SerializeField] private bool _isPlacing;
        [SerializeField] private Vector3Int _currentCell;

        // Cache per ottimizzazione Update
        private Vector3Int _lastCell = Vector3Int.one * -1000;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Indica se il placer è attualmente in modalità placement.
        /// </summary>
        public bool IsPlacing => _isPlacing && _ghostInstance != null;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_manager == null) _manager = GetComponent<BuildingManager>();
            if (_camera == null) _camera = Camera.main;
            
            ValidateDependencies();
        }

        private void Update()
        {
            if (!_isPlacing || _ghostInstance == null) return;
            UpdateGhostFollowMouse();
        }

        private void OnDestroy()
        {
            // Cleanup: distruggi ghost se ancora presente
            CleanupGhost();
            
            // Rimuovi preview dalla griglia
            if (_manager?.Grid != null)
            {
                _manager.Grid.SetCellsPreview(_currentCell, 0, 0, false);
            }
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

            CreateGhost(config);
            _selectedConfig = config;
            _isPlacing = true;
            _lastCell = Vector3Int.one * -1000; // Reset cache
            
            Debug.Log($"[BuildingPlacer] Placement iniziato: {config.name}");
        }

        /// <summary>
        /// Conferma il piazzamento dell'edificio nella posizione corrente se valida.
        /// </summary>
        public void ConfirmPlacement()
        {
            if (!_isPlacing || _ghostInstance == null)
            {
                Debug.LogWarning("[BuildingPlacer] Impossibile confermare: non in modalità placement.");
                return;
            }

            var config = _ghostInstance.Config;

            // Validazione finale
            if (!BuildingPlacementValidator.CanPlace(_manager.Grid, _manager.Economy, config, _currentCell))
            {
                Debug.Log("[BuildingPlacer] Conferma fallita: posizione non valida o risorse insufficienti.");
                return;
            }

            // Crea edificio reale
            var worldPos = _manager.Grid.CellToWorld(_currentCell);
            var building = _manager.Factory.CreateBuilding(config, worldPos, _manager.Root);
            
            if (building == null)
            {
                Debug.LogError("[BuildingPlacer] Creazione building fallita.");
                return;
            }

            // Spendi risorse e occupa celle
            _manager.Economy?.SpendResources(config.ToDictionary());
            _manager.Grid.OccupyCells(_currentCell, config.Width, config.Height, building);

            Debug.Log($"[BuildingPlacer] Edificio piazzato: {config.name} alla posizione {worldPos}");
            
            // Notifica evento
            BuildingEvents.OnBuildingPlaced?.Invoke(building);

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

            CleanupGhost();
            CleanupPreview();
            
            _isPlacing = false;
            _selectedConfig = null;
            
            Debug.Log("[BuildingPlacer] Placement annullato.");
        }
        
        #endregion

        #region Private Methods
        
        private void ValidateDependencies()
        {
            if (_manager == null)
            {
                Debug.LogError("[BuildingPlacer] BuildingManager non assegnato! Il componente non funzionerà correttamente.");
            }

            if (_camera == null)
            {
                Debug.LogError("[BuildingPlacer] Camera non trovata! Assegna una camera nell'Inspector.");
            }
        }

        private void CreateGhost(BuildingConfigSO config)
        {
            CleanupGhost(); // Cleanup precedente se esistente
            
            var ghostGo = Instantiate(config.Prefab, Vector3.zero, Quaternion.identity, transform);
            ghostGo.name = $"Ghost_{config.name}";
            
            _ghostInstance = ghostGo.AddComponent<BuildingGhost>();
            _ghostInstance.Init(config);
        }

        private void CleanupGhost()
        {
            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance.gameObject);
                _ghostInstance = null;
            }
        }

        private void CleanupPreview()
        {
            if (_manager?.Grid != null)
            {
                _manager.Grid.SetCellsPreview(_currentCell, 0, 0, false);
            }
        }

        private void UpdateGhostFollowMouse()
        {
            if (_camera == null || _manager?.Grid == null) return;

            // Conversione mouse → world → cell
            var mousePos = Input.mousePosition;
            var worldPos = _camera.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f;

            if (!_manager.Grid.TryWorldToCell(worldPos, out var cell))
            {
                return;
            }

            // Ottimizzazione: aggiorna preview solo se la cella è cambiata
            if (cell == _lastCell)
            {
                return;
            }

            _currentCell = cell;
            _lastCell = cell;
            
            // Aggiorna posizione ghost
            var snapPos = _manager.Grid.CellToWorld(cell);
            _ghostInstance.transform.position = snapPos;

            // Validazione e aggiornamento visuale
            bool isValid = BuildingPlacementValidator.CanPlace(
                _manager.Grid, 
                _manager.Economy, 
                _ghostInstance.Config, 
                cell
            );
            
            _ghostInstance.UpdateVisual(isValid, snapPos.y);
            _manager.Grid.SetCellsPreview(
                cell, 
                _ghostInstance.Config.Width, 
                _ghostInstance.Config.Height, 
                isValid
            );
        }
        
        #endregion
    }
}
