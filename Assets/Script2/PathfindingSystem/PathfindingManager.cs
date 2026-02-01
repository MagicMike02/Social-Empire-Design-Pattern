using UnityEngine;
using VContainer;
using Script2.BuildingSystem;
using Script2.GridSystem;
using Script2.Core.Events;
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
        private CachedPathfindingDecorator _cacheDecorator; // Reference per invalidazione
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            InitializePathfinding();
            SubscribeToGridEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGridEvents();
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
        /// </summary>
        private void SubscribeToGridEvents()
        {
            // Grid events (edifici)
            GlobalEventBus.Subscribe<CellsOccupiedEvent>(OnCellsOccupied);
            GlobalEventBus.Subscribe<CellsFreedEvent>(OnCellsFreed);
            
            // Resource events (risorse che bloccano walkability)
            GlobalEventBus.Subscribe<ResourceGeneratedEvent>(OnResourceGenerated);
            GlobalEventBus.Subscribe<ResourceCollectedEvent>(OnResourceCollected);
            
            Debug.Log("[PathfindingManager] ✓ Subscribed to Grid + Resource events (cache auto-invalidation)");
        }

        /// <summary>
        /// Disiscrivi eventi per prevenire memory leak.
        /// </summary>
        private void UnsubscribeFromGridEvents()
        {
            // Grid events
            GlobalEventBus.Unsubscribe<CellsOccupiedEvent>(OnCellsOccupied);
            GlobalEventBus.Unsubscribe<CellsFreedEvent>(OnCellsFreed);
            
            // Resource events
            GlobalEventBus.Unsubscribe<ResourceGeneratedEvent>(OnResourceGenerated);
            GlobalEventBus.Unsubscribe<ResourceCollectedEvent>(OnResourceCollected);
        }

        /// <summary>
        /// Handler: Celle occupate (edificio piazzato o risorsa spawned).
        /// Invalida cache perché la walkability è cambiata.
        /// </summary>
        private void OnCellsOccupied(CellsOccupiedEvent evt)
        {
            if (_cacheDecorator != null)
            {
                int cachedPaths = _cacheDecorator.GetCacheSize();
                _cacheDecorator.ClearCache();
                
                #if UNITY_EDITOR
                Debug.Log($"[PathfindingManager] Grid changed (occupied {evt.Width}x{evt.Height} cells) → Cache invalidated ({cachedPaths} paths cleared)");
                #endif
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
                int cachedPaths = _cacheDecorator.GetCacheSize();
                _cacheDecorator.ClearCache();
                
                #if UNITY_EDITOR
                Debug.Log($"[PathfindingManager] Grid changed (freed {evt.Width}x{evt.Height} cells) → Cache invalidated ({cachedPaths} paths cleared)");
                #endif
            }
        }

        /// <summary>
        /// Handler: Risorsa generata (albero, pietra, oro spawned).
        /// Invalida cache perché risorsa blocca walkability.
        /// NOTE: Silent durante initial spawn (300+ risorse) per evitare log flood.
        /// </summary>
        private void OnResourceGenerated(ResourceGeneratedEvent evt)
        {
            if (_cacheDecorator != null)
            {
                _cacheDecorator.ClearCache();
                
                // Log disabilitato: genera 300+ log durante initial spawn
                // #if UNITY_EDITOR
                // Debug.Log($"[PathfindingManager] Resource spawned at {evt.Position} → Cache invalidated");
                // #endif
            }
        }

        /// <summary>
        /// Handler: Risorsa raccolta (albero/pietra rimosso).
        /// Invalida cache perché cella ora è walkable.
        /// </summary>
        private void OnResourceCollected(ResourceCollectedEvent evt)
        {
            if (_cacheDecorator != null)
            {
                int cachedPaths = _cacheDecorator.GetCacheSize();
                _cacheDecorator.ClearCache();
                
                #if UNITY_EDITOR
                Debug.Log($"[PathfindingManager] Resource collected at {evt.Position} → Cache invalidated ({cachedPaths} paths cleared)");
                #endif
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
