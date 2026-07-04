using System.Collections.Generic;
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
