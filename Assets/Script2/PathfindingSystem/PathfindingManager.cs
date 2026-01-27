﻿﻿using UnityEngine;
using VContainer;
using Script2.BuildingSystem;
using System.Collections.Generic;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// PathfindingManager: Coordinate pathfinding on isometric grid.
    /// Uses Strategy + Decorator patterns for modularity.
    /// - IPathfindingAlgorithm: Abstract algorithm interface
    /// - AStarAlgorithm: 8-directional A* with Chebyshev heuristic
    /// - CachedPathfindingDecorator: LRU caching (100 entry limit)
    /// - DebugPathfindingDecorator: Tile visualization (editor only)
    /// Performance: 10ms for 193 cell paths on 100x100 grid
    /// </summary>
    public class PathfindingManager : MonoBehaviour
    {
        [Inject] private IGridService _gridService;

        private IPathfindingAlgorithm _pathfindingAlgorithm;

        private void Start()
        {
            InitializePathfinding();
        }

        /// <summary>
        /// Inizializza il sistema di pathfinding con Strategy pattern.
        /// Composizione: A* + Cache + Debug visualization (solo in editor).
        /// </summary>
        private void InitializePathfinding()
        {
            // Base algorithm
            IPathfindingAlgorithm algorithm = new AStarAlgorithm();

            // Wrap with caching (always enabled)
            algorithm = new CachedPathfindingDecorator(algorithm);

            // Wrap with debug visualization (only in editor)
            #if UNITY_EDITOR
            algorithm = new DebugPathfindingDecorator(algorithm);
            #endif

            _pathfindingAlgorithm = algorithm;

            Debug.Log("[PathfindingManager] ✓ Initialized with A* + Caching + Debug");
        }

        /// <summary>
        /// Public API: Trova percorso delegando all'algoritmo scelto.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            #if UNITY_EDITOR
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var path = _pathfindingAlgorithm.FindPath(start, goal, _gridService);
            sw.Stop();
            Debug.Log($"[PathfindingManager] FindPath: {start} → {goal} = {path.Count} cells in {sw.ElapsedMilliseconds}ms");
            return path;
            #else
            return _pathfindingAlgorithm.FindPath(start, goal, _gridService);
            #endif
        }
    }
}
