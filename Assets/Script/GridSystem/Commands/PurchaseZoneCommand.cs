using System.Collections.Generic;
using System.Threading.Tasks;
using Script.Core.Commands;
using Script.EconomySystem;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.GridSystem.Commands
{
    /// <summary>
    /// Command: Acquista e sblocca una zona.
    /// Execute: Valida zona + risorse → Spende risorse → Sblocca zona
    /// Undo: Riporta zona locked → Restituisce 100% risorse
    /// </summary>
    public class PurchaseZoneCommand : ICommand
    {
        #region Private Fields
        
        private readonly ZoneManager _zoneManager;
        private readonly GameEconomyManager _economy;
        private readonly Vector2Int _zoneCoord;
        private readonly Dictionary<ResourceType, int> _cost;

        private bool _wasUnlocked;
        
        #endregion

        #region Properties

        public CommandState State { get; set; } = CommandState.Pending;
        public string Description => $"Purchase zone at {_zoneCoord}";
        
        #endregion

        #region Initialization

        /// <summary>
        /// Costruttore Command.
        /// </summary>
        public PurchaseZoneCommand(
            ZoneManager zoneManager,
            GameEconomyManager economy,
            Vector2Int zoneCoord,
            Dictionary<ResourceType, int> cost)
        {
            _zoneManager = zoneManager;
            _economy = economy;
            _zoneCoord = zoneCoord;
            _cost = new Dictionary<ResourceType, int>(cost);
        }
        
        #endregion

        #region Core Functionality

        /// <summary>
        /// Esegue l'acquisto spendendo risorse e sbloccando le tile della zona.
        /// </summary>
        public bool Execute()
        {
            if (!_zoneManager.HasZone(_zoneCoord))
                return false;

            _wasUnlocked = _zoneManager.IsZoneUnlocked(_zoneCoord);
            if (_wasUnlocked)
                return false;

            if (!_economy.CanAfford(_cost))
                return false;

            if (!_economy.SpendResources(_cost))
                return false;

            _zoneManager.UnlockZone(_zoneCoord);
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

            // Step 2: Future — await _backendService.ConfirmPurchaseAsync(...)
            // Per ora conferma immediata
            await Task.CompletedTask;

#if UNITY_EDITOR
            Debug.Log($"[PurchaseZoneCommand] ✓ Async confirmed: {Description}");
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
            Debug.Log($"[PurchaseZoneCommand] ✓ Confirmed by server: {Description}");
#endif
        }

        /// <summary>
        /// Annulla l'espansione zona rendendola nuovamente inaccessibile e fornendo refund totale.
        /// </summary>
        public bool Undo()
        {
            if (!_zoneManager.HasZone(_zoneCoord))
                return false;

            // UNDO: Riporta zona locked
            _zoneManager.LockZone(_zoneCoord);

            // UNDO: Refund 100% risorse
            foreach (var resource in _cost)
            {
                _economy.AddResource(resource.Key, resource.Value);
            }

#if UNITY_EDITOR
            Debug.Log($"[PurchaseZoneCommand] ✓ Undone: {Description} (100% refund)");
#endif

            return true;
        }
        
        #endregion
    }
}
