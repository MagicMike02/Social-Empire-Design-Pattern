using System.Collections.Generic;
using UnityEngine;
using Script2.BuildingSystem;
using Script2.GridSystem;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// Decorator Pattern: Aggiunge debug visualization a qualsiasi algoritmo.
    /// Colora i tile del percorso (verde=start, rosso=goal, blu=path).
    /// Disabilita in build di produzione (wrapping condizionale).
    /// </summary>
    public class DebugPathfindingDecorator : IPathfindingAlgorithm
    {
        private readonly IPathfindingAlgorithm _innerAlgorithm;
        private TileManager _tileManager;

        public DebugPathfindingDecorator(IPathfindingAlgorithm algorithm)
        {
            _innerAlgorithm = algorithm ?? throw new System.ArgumentNullException(nameof(algorithm));
            _tileManager = Object.FindFirstObjectByType<TileManager>();
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
                _tileManager = Object.FindFirstObjectByType<TileManager>();
            }

            if (_tileManager == null)
            {
                Debug.LogError("[DebugPathfinding] TileManager not found");
                return;
            }

            var grid = _tileManager.GetGrid();
            if (grid == null)
            {
                Debug.LogError("[DebugPathfinding] Grid is null");
                return;
            }

            var pathColor = new Color(0f, 0.5f, 1f, 0.6f);   // Blu
            var startColor = new Color(0f, 1f, 0f, 0.8f);     // Verde
            var goalColor = new Color(1f, 0f, 0f, 0.8f);      // Rosso

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
