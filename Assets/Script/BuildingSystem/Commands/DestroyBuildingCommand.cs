using Script.Core.Commands;
using Script.EconomySystem;
using System.Threading.Tasks;
using UnityEngine;

namespace Script.BuildingSystem.Commands
{
    /// <summary>
    /// Command: Distruggi edificio esistente.
    /// Execute: Libera celle → Refund 50% risorse → Distruggi GameObject
    /// Undo: Ripiazza edificio → Occupa celle → Sottrae refund (inverti)
    ///
    /// DESIGN: Salva stato completo edificio per permettere Undo (rebuild).
    /// </summary>
    public class DestroyBuildingCommand : ICommand
    {
        #region Dependencies & Fields
        
        // Dependencies
        private readonly BuildingManager _buildingManager;
        private readonly IGridService _gridService;
        private readonly GameEconomyManager _economy;

        // Stato edificio (salvato per Undo)
        private readonly Building _originalBuilding;
        private readonly BuildingConfigSO _config;
        private readonly Vector3Int _gridPosition;
        private readonly Vector3 _worldPosition;

        // Stato dopo Execute (per Undo)
        private Building _rebuiltBuilding;
        
        #endregion

        #region Properties

        public CommandState State { get; set; } = CommandState.Pending;
        public string Description => $"Destroy {_config.name} at {_gridPosition}";
        
        #endregion

        #region Initialization

        /// <summary>
        /// Costruttore Command.
        /// </summary>
        /// <param name="buildingManager">Manager per Factory</param>
        /// <param name="gridService">Servizio griglia</param>
        /// <param name="economy">Manager economia</param>
        /// <param name="building">Edificio da distruggere (deve esistere!)</param>
        /// <param name="gridPosition">Posizione griglia edificio (origin cell)</param>
        public DestroyBuildingCommand(
            BuildingManager buildingManager,
            IGridService gridService,
            GameEconomyManager economy,
            Building building,
            Vector3Int gridPosition)
        {
            _buildingManager = buildingManager;
            _gridService = gridService;
            _economy = economy;
            
            // Salva stato edificio (necessario per Undo)
            _originalBuilding = building;
            _config = building.Config;
            _gridPosition = gridPosition; // Passato esplicitamente
            _worldPosition = building.transform.position;
        }
        
        #endregion

        #region Core Functionality

        /// <summary>
        /// Esegue la distruzione, rimborsa 50% delle risorse e libera le celle.
        /// </summary>
        public bool Execute()
        {
            if (_originalBuilding == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[DestroyBuildingCommand] Building already destroyed or null");
#endif
                return false;
            }

            // EXECUTE: Libera celle
            _gridService.FreeCells(_gridPosition, _config.Width, _config.Height);

            // EXECUTE: Restituisci 50% risorse (gameplay balance)
            var costs = _config.ToDictionary();
            foreach (var cost in costs)
            {
                int refundAmount = Mathf.FloorToInt(cost.Value * 0.5f);
                _economy.AddResource(cost.Key, refundAmount);
            }

            // EXECUTE: Distruggi GameObject
            Object.Destroy(_originalBuilding.gameObject);

            #if UNITY_EDITOR
            Debug.Log($"[DestroyBuildingCommand] ✓ Executed: {Description} (50% refund)");
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

            // Step 2: Future — await _backendService.ConfirmDestroyAsync(...)
            // Per ora conferma immediata
            await Task.CompletedTask;

#if UNITY_EDITOR
            Debug.Log($"[DestroyBuildingCommand] ✓ Async confirmed: {Description}");
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
            Debug.Log($"[DestroyBuildingCommand] ✓ Confirmed by server: {Description}");
#endif
        }

        /// <summary>
        /// Effettua il rollback della distruzione ri-creando l'edificio tramite la Factory.
        /// </summary>
        public bool Undo()
        {
            // UNDO: Ripiazza edificio (ricostruisci)
            _rebuiltBuilding = _buildingManager.Factory.CreateBuilding(_config, _worldPosition, _buildingManager.Root);

            if (_rebuiltBuilding == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[DestroyBuildingCommand] Failed to rebuild {_config.name} during undo!");
#endif
                return false;
            }

            // UNDO: Occupa celle
            _gridService.OccupyCells(_gridPosition, _config.Width, _config.Height, _rebuiltBuilding);

            // UNDO: Sottrai risorse restituite (inverti refund)
            // Questo ripristina economia è stato pre-destroy
            var costs = _config.ToDictionary();
            foreach (var cost in costs)
            {
                int refundAmount = Mathf.FloorToInt(cost.Value * 0.5f);
                bool removed = _economy.SpendResources(cost.Key, refundAmount);
                
                if (!removed)
                {
                    // Se player ha speso le risorse nel frattempo, warning
#if UNITY_EDITOR
                    Debug.LogWarning($"[DestroyBuildingCommand] Undo: Insufficient {cost.Key} to remove refund (player spent them)");
#endif
                }
            }

            #if UNITY_EDITOR
            Debug.Log($"[DestroyBuildingCommand] ✓ Undone: {Description} (building rebuilt)");
            #endif

            return true;
        }
        
        #endregion
    }
}
