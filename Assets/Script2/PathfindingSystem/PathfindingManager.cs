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
    /// </summary>
    public class PathfindingManager : MonoBehaviour
    {
        [Inject] private IGridService _gridService;

        [SerializeField] private bool _enableDebugVisualization;
        [Tooltip("Abilita visualizzazione debug del pathfinding anche in Build")]

        private IPathfindingAlgorithm _pathfindingAlgorithm;

        private void Start()
        {
            InitializePathfinding();
        }

        /// <summary>
        /// Inizializza il sistema di pathfinding con Strategy pattern.
        /// Composizione: A* + Cache + Debug visualization (abilitabile).
        /// </summary>
        private void InitializePathfinding()
        {
            // Base algorithm
            IPathfindingAlgorithm algorithm = new AStarAlgorithm();

            // Wrap with caching (always enabled)
            algorithm = new CachedPathfindingDecorator(algorithm);

            // Wrap with debug visualization (se abilitato in Inspector)
            if (_enableDebugVisualization)
            {
                algorithm = new DebugPathfindingDecorator(algorithm);
                Debug.Log("[PathfindingManager] ✓ Debug visualization ENABLED");
            }

            _pathfindingAlgorithm = algorithm;

            Debug.Log("[PathfindingManager] ✓ Initialized with A* + Caching" + 
                      (_enableDebugVisualization ? " + Debug" : ""));
        }

        /// <summary>
        /// Trova percorso delegando all'algoritmo scelto.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var path = _pathfindingAlgorithm.FindPath(start, goal, _gridService);
            sw.Stop();
            Debug.Log($"[PathfindingManager] FindPath: {start} → {goal} = {path.Count} cells in {sw.ElapsedMilliseconds}ms");
            return path;
        }
    }
}
