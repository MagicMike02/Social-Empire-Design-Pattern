using System.Collections.Generic;
using Script.BuildingSystem;
using Script.Core.Events;
using UnityEngine;

namespace Script.GridSystem
{
    /// <summary>
    /// Tiene traccia delle celle occupate senza conoscere la logica di validazione della griglia.
    /// </summary>
    public sealed class GridOccupancyTracker
    {
        private readonly Dictionary<Vector2Int, GameObject> _occupiedCells = new();

        public bool IsCellOccupied(Vector2Int cell)
        {
            return _occupiedCells.ContainsKey(cell);
        }

        public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var cell = new Vector2Int(originCell.x + dx, originCell.y + dy);
                    _occupiedCells[cell] = building.gameObject;
                }
            }

            GlobalEventBus.Publish(new CellsOccupiedEvent(
                originCell,
                width,
                height,
                building?.gameObject
            ));
        }

        public void FreeCells(Vector3Int originCell, int width, int height)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var cell = new Vector2Int(originCell.x + dx, originCell.y + dy);
                    _occupiedCells.Remove(cell);
                }
            }

            GlobalEventBus.Publish(new CellsFreedEvent(originCell, width, height));
        }

        public void OccupyCell(Vector2Int cell, GameObject context)
        {
            _occupiedCells[cell] = context;
        }

        public void FreeCell(Vector2Int cell)
        {
            _occupiedCells.Remove(cell);
        }

        public bool IsCellFree(Vector2Int cell)
        {
            return !_occupiedCells.ContainsKey(cell);
        }

        /// <summary>
        /// Restituisce una vista in sola lettura delle celle occupate.
        /// Non alloca: espone direttamente il dizionario interno come IReadOnlyDictionary.
        /// Il chiamante NON deve modificare la collezione ritornata.
        /// </summary>
        public IReadOnlyDictionary<Vector2Int, GameObject> GetSnapshot()
        {
            return _occupiedCells;
        }

        public void Clear()
        {
            _occupiedCells.Clear();
        }
    }
}
