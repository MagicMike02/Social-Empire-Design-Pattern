using System;
using System.Collections.Generic;
using Script.BuildingSystem;
using Script.GridSystem;
using UnityEngine;

namespace Script.PathfindingSystem
{
    /// <summary>
    /// Decorator Pattern: Aggiunge debug visualization a qualsiasi algoritmo.
    /// Colora i tile del percorso (verde=start, rosso=goal, blu=path).
    /// </summary>
    public class DebugPathfindingDecorator : IPathfindingAlgorithm
    {
        private readonly IPathfindingAlgorithm _innerAlgorithm;
        private readonly TileManager _tileManager;

        public DebugPathfindingDecorator(IPathfindingAlgorithm algorithm, TileManager tileManager)
        {
            _innerAlgorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            _tileManager = tileManager ?? throw new ArgumentNullException(nameof(tileManager));
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, IGridService gridService)
        {
            var path = _innerAlgorithm.FindPath(start, goal, gridService);

            #if UNITY_EDITOR
            VisualizePathDebug(path);
            #endif

            return path;
        }

        /// <summary>
        /// DEBUG ONLY: Visualizza il percorso colorando i tile.
        /// </summary>
        private void VisualizePathDebug(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                Debug.Log("[DebugPathfinding] No path to visualize");
                return;
            }

            if (_tileManager == null)
            {
                Debug.LogError("[DebugPathfinding] TileManager is null");
                return;
            }

            var grid = _tileManager.GetGrid();
            if (grid == null)
            {
                Debug.LogError("[DebugPathfinding] Grid is null");
                return;
            }

            var pathColor = new Color(0f, 0.5f, 1f, 1.0f);   // Blu 
            var startColor = new Color(0f, 1f, 0f, 1.0f);     // Verde 
            var goalColor = new Color(1f, 0f, 0f, 1.0f);      // Rosso 

            for (int i = 0; i < path.Count; i++)
            {
                var cell = path[i];
                var tile = grid.GetValue(cell.x, cell.y);
                
                if (tile == null) continue;

                if (i == 0)
                    tile.DebugSetColor(startColor);
                else if (i == path.Count - 1)
                    tile.DebugSetColor(goalColor);
                else
                    tile.DebugSetColor(pathColor);
            }

            Debug.Log($"[DebugPathfinding] Visualized {path.Count} cells");
        }
    }
}
