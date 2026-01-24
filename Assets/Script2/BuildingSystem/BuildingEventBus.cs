using System;
using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Event Bus per gli eventi del sistema Building.
    /// BEST PRACTICE: Sostituisce BuildingEvents static class.
    /// 
    /// Vantaggi rispetto a static events:
    /// - Auto-cleanup quando GameObject distrutto
    /// - Testabile (può essere mockato)
    /// - No memory leaks su scene transitions
    /// - Dependency Injection friendly
    /// </summary>
    public class BuildingEventBus : MonoBehaviour
    {
        /// <summary>
        /// Invocato quando un edificio viene selezionato dall'utente.
        /// </summary>
        public event Action<Building> OnBuildingSelected;
        
        /// <summary>
        /// Invocato quando un edificio viene distrutto/rimosso dalla scena.
        /// </summary>
        public event Action<Building> OnBuildingDestroyed;
        
        /// <summary>
        /// Invocato quando un edificio viene piazzato con successo sulla griglia.
        /// </summary>
        public event Action<Building> OnBuildingPlaced;

        private void OnDestroy()
        {
            // Auto-cleanup: niente EventCleanupManager necessario!
            OnBuildingSelected = null;
            OnBuildingDestroyed = null;
            OnBuildingPlaced = null;
        }

        /// <summary>
        /// Trigger evento piazzamento edificio.
        /// </summary>
        public void RaiseBuildingPlaced(Building building)
        {
            OnBuildingPlaced?.Invoke(building);
        }

        /// <summary>
        /// Trigger evento selezione edificio.
        /// </summary>
        public void RaiseBuildingSelected(Building building)
        {
            OnBuildingSelected?.Invoke(building);
        }

        /// <summary>
        /// Trigger evento distruzione edificio.
        /// </summary>
        public void RaiseBuildingDestroyed(Building building)
        {
            OnBuildingDestroyed?.Invoke(building);
        }
    }
}
