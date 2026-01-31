﻿﻿﻿using UnityEngine;
using VContainer;
using Script2.BuildingSystem;
using Script2.GridSystem;
using System.Collections.Generic;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// PathfindingManager: Coordinate pathfinding on isometric grid.
    /// Uses Strategy + Decorator patterns for modularity.
    /// REFACTORED: Usa Dependency Injection per IGridService e TileManager.
    /// - IPathfindingAlgorithm: Abstract algorithm interface
    /// - AStarAlgorithm: 8-directional A* with Chebyshev heuristic
    /// - CachedPathfindingDecorator: LRU caching (100 entry limit)
    /// - DebugPathfindingDecorator: Tile visualization (editor only)
    /// </summary>
    public class PathfindingManager : MonoBehaviour
    {
        #region Dependencies (Injected by VContainer)
        
        private IGridService _gridService;
        private TileManager _tileManager;

        [Inject]
        public void Construct(IGridService gridService, TileManager tileManager)
        {
            _gridService = gridService;
            _tileManager = tileManager;
        }
        
        #endregion

        #region Configuration
        
        [SerializeField] private bool _enableDebugVisualization;
        [Tooltip("Abilita visualizzazione debug del pathfinding anche in Build")]
        
        #endregion

        #region Private Fields
        
        private IPathfindingAlgorithm _pathfindingAlgorithm;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            InitializePathfinding();
        }
        
        #endregion

        #region Initialization

        /// <summary>
        /// Inizializza il sistema di pathfinding con Strategy pattern.
        /// Composizione: A* + Cache + Debug visualization (abilitabile).
        /// </summary>
        private void InitializePathfinding()
        {
            // Validate dependencies
            if (_gridService == null)
            {
                Debug.LogError("[PathfindingManager] IGridService non iniettato! VContainer dovrebbe averlo fornito.");
                return;
            }

            if (_tileManager == null)
            {
                Debug.LogError("[PathfindingManager] TileManager non iniettato! VContainer dovrebbe averlo fornito.");
                return;
            }

            // Base algorithm
            IPathfindingAlgorithm algorithm = new AStarAlgorithm();

            // Wrap with caching (always enabled)
            algorithm = new CachedPathfindingDecorator(algorithm);

            // Wrap with debug visualization (se abilitato in Inspector)
            if (_enableDebugVisualization)
            {
                algorithm = new DebugPathfindingDecorator(algorithm, _tileManager);
                Debug.Log("[PathfindingManager] ✓ Debug visualization ENABLED");
            }

            _pathfindingAlgorithm = algorithm;

            Debug.Log("[PathfindingManager] ✓ Initialized with A* + Caching" + 
                      (_enableDebugVisualization ? " + Debug" : ""));
        }
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Trova percorso delegando all'algoritmo scelto.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            if (_pathfindingAlgorithm == null)
            {
                Debug.LogError("[PathfindingManager] Pathfinding non inizializzato!");
                return new List<Vector2Int>();
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var path = _pathfindingAlgorithm.FindPath(start, goal, _gridService);
            sw.Stop();
            
            #if UNITY_EDITOR
            Debug.Log($"[PathfindingManager] FindPath: {start} → {goal} = {path.Count} cells in {sw.ElapsedMilliseconds}ms");
            #endif
            
            return path;
        }
        
        #endregion
    }
}
