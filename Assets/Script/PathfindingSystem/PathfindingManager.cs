using System.Collections.Generic;
using System.Diagnostics;
using Script.BuildingSystem;
using Script.Core.Events;
using Script.GridSystem;
using UnityEngine;
using VContainer;
using Debug = UnityEngine.Debug;

namespace Script.PathfindingSystem
{
    /// <summary>
    /// PathfindingManager: Coordinate pathfinding on isometric grid.
    /// Uses Strategy + Decorator patterns for modularity.
    /// - IPathfindingAlgorithm: Abstract algorithm interface
    /// - AStarAlgorithm: 8-directional A* with Chebyshev heuristic
    /// - CachedPathfindingDecorator: LRU caching (500 entry limit)
    /// - DebugPathfindingDecorator: Tile visualization (editor only)
    /// Cache invalidation: Solo su CellsOccupiedEvent/CellsFreedEvent
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
        private CachedPathfindingDecorator _cacheDecorator; // Reference per invalidazione
        
        #endregion

        #region Unity Lifecycle
        
        private void OnEnable()
        {
            SubscribeToGridEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromGridEvents();
        }

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

            // Wrap with caching (always enabled) - salva reference per invalidazione
            _cacheDecorator = new CachedPathfindingDecorator(algorithm);
            algorithm = _cacheDecorator;

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

        #region Event Handling

        /// <summary>
        /// Sottoscrivi eventi Grid per invalidare cache quando celle cambiano.
        /// NOTE: Resource events NON servono più (gestiti da CellsOccupied/FreedEvent)
        /// </summary>
        private void SubscribeToGridEvents()
        {
            // Grid events (edifici e risorse)
            GlobalEventBus.Subscribe<CellsOccupiedEvent>(OnCellsOccupied);
            GlobalEventBus.Subscribe<CellsFreedEvent>(OnCellsFreed);
            
            Debug.Log("[PathfindingManager] ✓ Subscribed to Grid events (cache auto-invalidation)");
        }

        /// <summary>
        /// Disiscrivi eventi per prevenire memory leak.
        /// </summary>
        private void UnsubscribeFromGridEvents()
        {
            GlobalEventBus.Unsubscribe<CellsOccupiedEvent>(OnCellsOccupied);
            GlobalEventBus.Unsubscribe<CellsFreedEvent>(OnCellsFreed);
        }

        /// <summary>
        /// Handler: Celle occupate (edificio piazzato).
        /// Invalida cache perché la walkability è cambiata.
        /// </summary>
        private void OnCellsOccupied(CellsOccupiedEvent evt)
        {
            if (_cacheDecorator != null)
            {
                _cacheDecorator.ClearCache();
            }
        }

        /// <summary>
        /// Handler: Celle liberate (edificio distrutto).
        /// Invalida cache perché la walkability è cambiata.
        /// </summary>
        private void OnCellsFreed(CellsFreedEvent evt)
        {
            if (_cacheDecorator != null)
            {
                _cacheDecorator.ClearCache();
            }
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

            var sw = Stopwatch.StartNew();
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
