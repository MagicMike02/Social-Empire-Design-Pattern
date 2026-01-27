using System.Collections.Generic;
using UnityEngine;
using Script2.BuildingSystem;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// Strategy Pattern: Interfaccia per algoritmi di pathfinding intercambiabili.
    /// Permette di swappare A*, JPS, Theta* senza modificare il codice cliente.
    /// </summary>
    public interface IPathfindingAlgorithm
    {
        /// <summary>
        /// Trova il percorso tra start e goal.
        /// </summary>
        /// <param name="start">Posizione di partenza</param>
        /// <param name="goal">Posizione obiettivo</param>
        /// <param name="gridService">Servizio di griglia per accesso dati</param>
        /// <returns>Lista di celle (vuota se nessun percorso)</returns>
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, IGridService gridService);
    }
}
