using UnityEngine;
using Script2.BuildingSystem;
using Script2.GridSystem;
using Script2.InputSystem;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// DEBUG TEST SCRIPT: Testare pathfinding senza Units.
    /// Click su primo tile = start
    /// Click su secondo tile = goal
    /// Visualizza il percorso colorando i tile (blu = path, verde = start, rosso = goal)
    /// </summary>
    public class PathfindingDebugTester : MonoBehaviour
    {
        private static PathfindingDebugTester _instance;

        private IGridService _gridService;
        private PathfindingManager _pathfinding;
        private InputManager _input;

        private Vector2Int? _startCell;
        private Vector2Int? _goalCell;

        [SerializeField] private bool _debugEnabled = true;

        private void Awake()
        {
            // Singleton pattern - distruggi duplicati
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[PathfindingDebugTester] Duplicate instance detected - destroying this one");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            if (_debugEnabled)
            {
                // Ottieni dipendenze manualmente
                _gridService = FindFirstObjectByType<GridManager>();
                _pathfinding = FindFirstObjectByType<PathfindingManager>();
                _input = FindFirstObjectByType<InputManager>();

                if (_gridService == null)
                {
                    Debug.LogError("[PathfindingDebugTester] GridManager not found in scene!");
                    _debugEnabled = false;
                    return;
                }

                if (_pathfinding == null)
                {
                    Debug.LogError("[PathfindingDebugTester] PathfindingManager not found in scene!");
                    _debugEnabled = false;
                    return;
                }

                Debug.Log("[PathfindingDebugTester] ✓ Initialized");
                
                // Hook sui tile clicks
                SubscribeToTileClicks();
            }
        }

        private void SubscribeToTileClicks()
        {
            // Sottoscrivi al click centralizato di InputManager
            if (_input != null)
                _input.OnTileClicked += OnTileClicked;
        }

        private void OnTileClicked(Tile tile)
        {
            // Collider-First approach: read grid position directly from tile (Source of Truth)
            Vector2Int cell = tile.GridPosition;
            HandleTileClick(cell);
        }

        private void Update()
        {
            if (!_debugEnabled) return;

            // Right-click = reset
            if (Input.GetMouseButtonDown(1))
            {
                HandleReset();
            }
        }

        private void HandleTileClick(Vector2Int cell)
        {
            // Primo click = start
            if (_startCell == null)
            {
                _startCell = cell;
                Debug.Log($"[PathfindingDebugTester] ✓ START: {cell}");
                ColorStartCell(cell);  // Colora tile VERDE 🟢
                return;
            }

            // 🖱️ FASE 2: SECONDO CLICK → Imposta GOAL e calcola percorso
            if (_goalCell == null)
            {
                _goalCell = cell;
                Debug.Log($"[PathfindingDebugTester] ✓ GOAL: {cell}");
                TestPathfinding(_startCell.Value, _goalCell.Value);  // Calcola A*
                return;
            }

            // 🖱️ FASE 3: TERZO CLICK → Reset tutto e ricomincia
            Debug.Log("[PathfindingDebugTester] ✓ Reset - clearing path visualization");
            ClearPathVisualization();
            _startCell = null;
            _goalCell = null;
        }

        private void ColorStartCell(Vector2Int startCell)
        {
            var tileManager = FindFirstObjectByType<TileManager>();
            if (tileManager == null) return;

            var grid = tileManager.GetGrid();
            if (grid == null) return;

            var tile = grid.GetValue(startCell.x, startCell.y);
            if (tile != null)
            {
                var startColor = new Color(0f, 1f, 0f, 0.8f); // Verde
                tile.DebugSetColor(startColor);
                Debug.Log($"[PathfindingDebugTester] START cell {startCell} colored GREEN");
            }
        }

        private void ClearPathVisualization()
        {
            var tileManager = FindFirstObjectByType<TileManager>();
            if (tileManager == null) return;

            var grid = tileManager.GetGrid();
            if (grid == null) return;

            // Reset tutti i tile
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    var tile = grid.GetValue(x, y);
                    if (tile != null)
                    {
                        tile.ResetTint();
                    }
                }
            }

            Debug.Log("[PathfindingDebugTester] ✓ All tiles reset");
        }

        private void HandleReset()
        {
            ClearPathVisualization();
            _startCell = null;
            _goalCell = null;
            Debug.Log("[PathfindingDebugTester] ✓ Reset via right-click. Ready for new START");
        }

        private void TestPathfinding(Vector2Int start, Vector2Int goal)
        {
            Debug.Log($"[PathfindingDebugTester] 🔍 Pathfinding: {start} → {goal}");

            // DEBUG: Verifica walkability
            bool startWalkable = _gridService.IsCellWalkable(start);
            bool goalWalkable = _gridService.IsCellWalkable(goal);
            Debug.Log($"[PathfindingDebugTester] Walkability - START: {startWalkable}, GOAL: {goalWalkable}");

            if (!startWalkable)
                Debug.LogWarning($"[PathfindingDebugTester] ⚠️ START cell {start} is NOT walkable!");
            if (!goalWalkable)
                Debug.LogWarning($"[PathfindingDebugTester] ⚠️ GOAL cell {goal} is NOT walkable!");

            // Usa versione SYNC per testing
            var path = _pathfinding.FindPath(start, goal);

            // Visualization happens automatically via DebugPathfindingDecorator in editor
            
            if (path.Count == 0)
            {
                Debug.LogWarning("[PathfindingDebugTester] ❌ No path found!");
            }
            else
            {
                Debug.Log($"[PathfindingDebugTester] ✓ Path length: {path.Count} cells");
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.OnTileClicked -= OnTileClicked;
        }
    }
}
