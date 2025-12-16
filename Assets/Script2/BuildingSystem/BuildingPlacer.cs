﻿﻿﻿using UnityEngine;
using Script2.Common;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Gestisce il processo di piazzamento degli edifici con preview visiva e validazione in tempo reale.
    /// REFACTORED: Usa PreviewSystem dedicato per eliminare glitch e fix colori viola.
    /// </summary>
    public sealed class BuildingPlacer : MonoBehaviour
    {
        #region Fields
        
        [Header("Dependencies")]
        [SerializeField] private BuildingManager _manager;
        [SerializeField] private Camera _camera;
        [SerializeField] private PreviewSystem _previewSystem;

        [Header("State (Debug Only)")]
        [SerializeField] private BuildingConfigSO _selectedConfig;
        [SerializeField] private bool _isPlacing;
        [SerializeField] private Vector3Int _currentCell;

        // Cache per ottimizzazione Update (FIX GLITCH)
        private Vector3Int _lastCell = Vector3Int.one * -1000;
        private bool _lastValidState = true;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Indica se il placer è attualmente in modalità placement.
        /// </summary>
        public bool IsPlacing => _isPlacing;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_manager == null) _manager = GetComponent<BuildingManager>();
            if (_camera == null) _camera = Camera.main;
            if (_previewSystem == null) _previewSystem = GetComponent<PreviewSystem>();
            
            ValidateDependencies();
        }

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
            
            Debug.Log($"[BuildingPlacer] Placement iniziato: {config.name}");
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
                Debug.Log("[BuildingPlacer] Conferma fallita: posizione non valida o risorse insufficienti.");
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

            // Nascondi preview edificio
            if (_previewSystem != null)
            {
                _previewSystem.HidePreview();
            }

            // Pulisci preview griglia tile
            CleanupGridPreview();
            
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

            if (_previewSystem == null)
            {
                Debug.LogError("[BuildingPlacer] PreviewSystem non trovato! Aggiungi il componente PreviewSystem allo stesso GameObject.");
            }
        }

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

            // Conversione mouse → world → cell
            var mousePos = Input.mousePosition;
            var worldPos = _camera.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f;

            if (!_manager.Grid.TryWorldToCell(worldPos, out var cell))
            {
                return;
            }

            // OTTIMIZZAZIONE CRITICA: Aggiorna solo se cella cambiata (FIX GLITCH)
            if (cell == _lastCell)
            {
                return;
            }

            _currentCell = cell;
            
            // Calcola posizione world snappata
            var snapPos = _manager.Grid.CellToWorld(cell);

            // Validazione
            bool isValid = CanPlaceBuilding(_selectedConfig, cell);

            // Aggiorna preview edificio SOLO se cella cambiata (FIX GLITCH)
            bool updated = _previewSystem.UpdatePreviewIfCellChanged(cell, snapPos, isValid);

            // Se prima volta o cella cambiata, mostra preview
            if (_lastCell.x == -1000 || updated)
            {
                _previewSystem.ShowPreview(_selectedConfig.Prefab, snapPos, isValid);
            }

            // Aggiorna preview griglia tile SOLO se cella cambiata (FIX GLITCH)
            if (updated || _lastValidState != isValid)
            {
                _manager.Grid.SetCellsPreview(cell, _selectedConfig.Width, _selectedConfig.Height, isValid);
                _lastValidState = isValid;
            }

            _lastCell = cell;
        }

        /// <summary>
        /// Validazione consolidata del piazzamento edifici.
        /// Verifica: 1) Celle libere, 2) Risorse sufficienti
        /// (Consolidato da BuildingPlacementValidator)
        /// </summary>
        private bool CanPlaceBuilding(BuildingConfigSO config, Vector3Int originCell)
        {
            if (_manager?.Grid == null || config == null)
            {
                return false;
            }

            // Controllo 1: Celle libere
            bool cellsFree = _manager.Grid.AreCellsFree(originCell, config.Width, config.Height);
            
            // Controllo 2: Risorse sufficienti
            bool canAfford = _manager.Economy == null || _manager.Economy.CanAfford(config.ToDictionary());
            
            return cellsFree && canAfford;
        }
        
        #endregion
    }
}
