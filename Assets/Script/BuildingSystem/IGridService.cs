using System.Collections.Generic;
using UnityEngine;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Interfaccia per servizi di griglia.
    /// Disaccoppia BuildingSystem da implementazione concreta di GridManager.
    /// Permette testabilità e sostituibilità dell'implementazione.
    /// </summary>
    public interface IGridService
    {
        
        int Width { get; }
        int Height { get; }
        /// <summary>
        /// Converte una posizione world in coordinate di cella della griglia.
        /// </summary>
        /// <param name="worldPos">Posizione world</param>
        /// <param name="cell">Output: coordinate cella (se successo)</param>
        /// <returns>True se la conversione ha successo e la cella è valida</returns>
        bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell);
        
        /// <summary>
        /// Converte coordinate di cella in posizione world.
        /// </summary>
        /// <param name="cell">Coordinate cella</param>
        /// <returns>Posizione world center della cella</returns>
        Vector3 CellToWorld(Vector3Int cell);
        
        /// <summary>
        /// Verifica se un'area di celle è libera e disponibile per il piazzamento.
        /// </summary>
        /// <param name="originCell">Cella di origine (angolo bottom-left)</param>
        /// <param name="width">Larghezza in celle</param>
        /// <param name="height">Altezza in celle</param>
        /// <returns>True se tutte le celle sono libere, False altrimenti</returns>
        bool AreCellsFree(Vector3Int originCell, int width, int height);
        
        /// <summary>
        /// Marca un'area di celle come occupate da un edificio.
        /// </summary>
        /// <param name="originCell">Cella di origine</param>
        /// <param name="width">Larghezza in celle</param>
        /// <param name="height">Altezza in celle</param>
        /// <param name="building">Riferimento all'edificio che occupa le celle</param>
        void OccupyCells(Vector3Int originCell, int width, int height, Building building);
        
        /// <summary>
        /// Libera un'area di celle precedentemente occupate.
        /// </summary>
        /// <param name="originCell">Cella di origine</param>
        /// <param name="width">Larghezza in celle</param>
        /// <param name="height">Altezza in celle</param>
        void FreeCells(Vector3Int originCell, int width, int height);
        
        /// <summary>
        /// Imposta la preview visuale su un'area di celle.
        /// Mostra feedback visivo (verde/rosso) durante il piazzamento.
        /// </summary>
        /// <param name="originCell">Cella di origine</param>
        /// <param name="width">Larghezza in celle (0 per cancellare preview)</param>
        /// <param name="height">Altezza in celle (0 per cancellare preview)</param>
        /// <param name="isValid">True per preview verde (valido), False per rosso (invalido)</param>
        void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid);
        
        // ========== PATHFINDING SUPPORT (SPRINT 1) ==========
        
        /// <summary>
        /// Verifica se una cella è percorribile da un'unità.
        /// Una cella è walkable se: TileState.Unlocked + non occupata + no risorse.
        /// </summary>
        /// <param name="cell">Coordinate cella da verificare</param>
        /// <returns>True se la cella è percorribile, False altrimenti</returns>
        bool IsCellWalkable(Vector2Int cell);
        
        /// <summary>
        /// Riempie un buffer con le celle vicine percorribili senza allocare.
        /// Utilizzato da PathfindingManager per A* neighbor expansion.
        /// </summary>
        /// <param name="cell">Cella di partenza</param>
        /// <param name="neighbors">Buffer di output da riempire</param>
        void GetWalkableNeighbors(Vector2Int cell, List<Vector2Int> neighbors);
    }
}
