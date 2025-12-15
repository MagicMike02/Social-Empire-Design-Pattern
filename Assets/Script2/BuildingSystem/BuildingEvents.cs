﻿using System;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Hub centralizzato per eventi del sistema Building.
    /// NOTA: Gli eventi statici devono essere gestiti con cura per evitare memory leak.
    /// Assicurarsi di disiccrivere gli handler in OnDestroy/OnDisable.
    /// 
    /// Considerare in futuro la migrazione a un EventBus non statico o ScriptableObject-based events.
    /// </summary>
    public static class BuildingEvents
    {
        /// <summary>
        /// Invocato quando un edificio viene selezionato dall'utente.
        /// </summary>
        public static Action<Building> OnBuildingSelected;
        
        /// <summary>
        /// Invocato quando un edificio viene distrutto/rimosso dalla scena.
        /// </summary>
        public static Action<Building> OnBuildingDestroyed;
        
        /// <summary>
        /// Invocato quando un edificio viene piazzato con successo sulla griglia.
        /// </summary>
        public static Action<Building> OnBuildingPlaced;

        /// <summary>
        /// Pulisce tutti gli eventi sottoscritti.
        /// ATTENZIONE: Usare solo per cleanup globale (es. cambio scena, reset).
        /// </summary>
        public static void ClearAllEvents()
        {
            OnBuildingSelected = null;
            OnBuildingDestroyed = null;
            OnBuildingPlaced = null;
        }
    }
}
