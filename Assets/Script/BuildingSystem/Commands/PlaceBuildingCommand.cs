using Script.Core.Commands;
using Script.EconomySystem;
using System.Threading.Tasks;
using UnityEngine;

namespace Script.BuildingSystem.Commands
{
    /// <summary>
    /// Command: Piazza edificio sulla griglia.
    /// Execute: Valida celle + risorse → Piazza edificio → Spende risorse → Occupa celle
    /// Undo: Rimuove edificio → Restituisce 100% risorse → Libera celle
    ///
    /// DESIGN: Dependencies iniettate via costruttore (no static, testabile).
    /// </summary>
    public class PlaceBuildingCommand : ICommand
    {
        #region Dependencies & Fields
        
        // Dependencies (iniettate da BuildingPlacer)
        private readonly BuildingManager _buildingManager;
        private readonly IGridService _gridService;
        private readonly GameEconomyManager _economy;

        // Command parameters (immutabili)
        private readonly BuildingConfigSO _config;
        private readonly Vector3Int _gridPosition;

        // Command state (salvato dopo Execute per Undo)
        private Building _placedBuilding;
        private Vector3 _worldPosition;
        
        #endregion

        #region Properties

        public CommandState State { get; set; } = CommandState.Pending;
        public string Description => $"Place {_config.name} at {_gridPosition}";
        
        #endregion

        #region Initialization

        /// <summary>
        /// Costruttore Command.
        /// </summary>
        /// <param name="buildingManager">Manager per Factory e utilities</param>
        /// <param name="gridService">Servizio griglia (validazione + occupazione)</param>
        /// <param name="economy">Manager economia (validazione risorse + spesa)</param>
        /// <param name="config">Configurazione edificio da piazzare</param>
        /// <param name="gridPosition">Posizione griglia (origin cell)</param>
        public PlaceBuildingCommand(
            BuildingManager buildingManager,
            IGridService gridService,
            GameEconomyManager economy,
            BuildingConfigSO config,
            Vector3Int gridPosition)
        {
            _buildingManager = buildingManager;
            _gridService = gridService;
            _economy = economy;
            _config = config;
            _gridPosition = gridPosition;
        }
        
        #endregion
        
        #region Core Functionality

        /// <summary>
        /// Esegue il comando di piazzamento previa validazione grid e risorse.
        /// </summary>
        public bool Execute()
        {
            // VALIDATION 1: Celle libere?
            if (!_gridService.AreCellsFree(_gridPosition, _config.Width, _config.Height))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[PlaceBuildingCommand] Cannot place {_config.name}: cells occupied at {_gridPosition}");
#endif
                return false;
            }

            // VALIDATION 2: Risorse sufficienti?
            var costs = _config.ToDictionary();
            if (!_economy.CanAfford(costs))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[PlaceBuildingCommand] Cannot afford {_config.name}: insufficient resources");
#endif
                return false;
            }

            // EXECUTE: Calcola world position
            _worldPosition = _gridService.CellToWorld(_gridPosition);

            // EXECUTE: Crea edificio via Factory (usa BuildingManager.Root come parent)
            _placedBuilding = _buildingManager.Factory.CreateBuilding(_config, _worldPosition, _buildingManager.Root);

            if (_placedBuilding == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[PlaceBuildingCommand] Factory failed to create {_config.name}!");
#endif
                return false;
            }

            // EXECUTE: Spendi risorse
            bool spentResources = _economy.SpendResources(costs);
            if (!spentResources)
            {
                // Rollback: Distruggi edificio se spesa fallisce 
                Object.Destroy(_placedBuilding.gameObject);
                _placedBuilding = null;
#if UNITY_EDITOR
                Debug.LogError($"[PlaceBuildingCommand] Failed to spend resources (rollback executed)");
#endif
                return false;
            }

            // EXECUTE: Occupa celle
            _gridService.OccupyCells(_gridPosition, _config.Width, _config.Height, _placedBuilding);

            #if UNITY_EDITOR
            Debug.Log($"[PlaceBuildingCommand] ✓ Executed: {Description}");
            #endif

            return true;
        }

        /// <summary>
        /// Esegue il comando in modalità asincrona con optimistic update + conferma.
        /// Per ora: esegue l'optimistic update (Execute) e conferma immediatamente.
        /// In futuro: attenderà la conferma da IBackendService.
        /// </summary>
        public async Task<bool> ExecuteAsync()
        {
            // Step 1: Optimistic update (validazione + esecuzione sincrona)
            bool success = Execute();
            if (!success) return false;

            // Step 2: Future — await _backendService.ConfirmBuildAsync(...)
            // Per ora conferma immediata
            await Task.CompletedTask;

#if UNITY_EDITOR
            Debug.Log($"[PlaceBuildingCommand] ✓ Async confirmed: {Description}");
#endif

            return true;
        }

        /// <summary>
        /// Conferma il comando dopo ricezione conferma server.
        /// Chiamato da CommandHistory quando il backend conferma l'operazione.
        /// Sincronizza valori autoritativi dal server (es. saldo oro).
        /// </summary>
        public void Confirm()
        {
            State = CommandState.Confirmed;
            
            // Future: sincronizza valori autoritativi dal server
            // Es: _economy.SyncGoldFromServer(serverGoldAmount);
            
#if UNITY_EDITOR
            Debug.Log($"[PlaceBuildingCommand] ✓ Confirmed by server: {Description}");
#endif
        }

        /// <summary>
        /// Esegue l'annullamento dell'operazione, distruggendo il building e rimborsando il giocatore 100%.
        /// </summary>
        public bool Undo()
        {
            if (_placedBuilding == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[PlaceBuildingCommand] Cannot undo: building was never placed or already destroyed");
#endif
                return false;
            }

            // UNDO: Libera celle
            _gridService.FreeCells(_gridPosition, _config.Width, _config.Height);

            // UNDO: Restituisci 100% risorse (undo = rollback completo)
            var costs = _config.ToDictionary();
            foreach (var cost in costs)
            {
                _economy.AddResource(cost.Key, cost.Value);
            }

            // UNDO: Distruggi GameObject
            Object.Destroy(_placedBuilding.gameObject);
            _placedBuilding = null;

#if UNITY_EDITOR
            Debug.Log($"[PlaceBuildingCommand] ✓ Undone: {Description} (100% refund)");
#endif

            return true;
        }
        
        #endregion
    }
}
