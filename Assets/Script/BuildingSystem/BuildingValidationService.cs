using UnityEngine;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Servizio di validazione del piazzamento edifici (pure C#, no MonoBehaviour).
    /// Verifica celle libere e risorse sufficienti.
    /// </summary>
    public sealed class BuildingValidationService
    {
        private readonly BuildingManager _manager;

        public BuildingValidationService(BuildingManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Validazione consolidata: 1) Celle libere, 2) Risorse sufficienti.
        /// </summary>
        public bool CanPlaceBuilding(BuildingConfigSO config, Vector3Int originCell)
        {
            if (_manager?.Grid == null || config == null)
            {
                return false;
            }

            bool cellsFree = _manager.Grid.AreCellsFree(originCell, config.Width, config.Height);
            bool canAfford = _manager.Economy == null || _manager.Economy.CanAfford(config.ToDictionary());

            return cellsFree && canAfford;
        }
    }
}
