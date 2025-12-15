﻿using Script2.Economy;
using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Utilità statica per validare il piazzamento di edifici.
    /// Verifica sia disponibilità celle sia risorse necessarie.
    /// </summary>
    public static class BuildingPlacementValidator
    {
        /// <summary>
        /// Controlla se un edificio può essere piazzato in una posizione specifica.
        /// Verifica:
        /// 1. Celle della griglia siano libere e sbloccate
        /// 2. Risorse sufficienti per costruire l'edificio
        /// </summary>
        /// <param name="grid">Servizio griglia per controllo celle</param>
        /// <param name="economy">Manager risorse per controllo costi</param>
        /// <param name="config">Configurazione edificio da validare</param>
        /// <param name="originCell">Cella di origine (angolo bottom-left)</param>
        /// <returns>True se il piazzamento è valido, altrimenti False</returns>
        public static bool CanPlace(IGridService grid, GameEconomyManager economy, BuildingConfigSO config, Vector3Int originCell)
        {
            if (grid == null || config == null)
            {
                return false;
            }

            // Controllo 1: Celle libere
            bool cellsFree = grid.AreCellsFree(originCell, config.Width, config.Height);
            
            // Controllo 2: Risorse sufficienti (se economy è presente)
            bool canAfford = economy == null || economy.CanAfford(config.ToDictionary());
            
            return cellsFree && canAfford;
        }
    }
}
