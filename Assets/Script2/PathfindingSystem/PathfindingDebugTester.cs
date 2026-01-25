using UnityEngine;
using VContainer;
using Script2.PathfindingSystem;
using Script2.BuildingSystem;
using Script2.InputSystem;
using Script2.GridSystem;

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
        [Inject] private IGridService _gridService;
        [Inject] private PathfindingManager _pathfinding;

        private Vector2Int? _startCell = null;
        private Vector2Int? _goalCell = null;

        [SerializeField] private bool _debugEnabled = true;

        private void Start()
        {
            if (_debugEnabled)
            {
                Debug.Log("[PathfindingDebugTester] ✓ Initialized");
                Debug.Log("[PathfindingDebugTester] Instructions: Click first tile for START, second tile for GOAL, right-click to reset");
                
                // Hook into InputManager to capture tile clicks
                HookIntoTileClicks();
            }
        }

        private void HookIntoTileClicks()
        {
            // Find all tiles in scene and hook their click events
            var tiles = FindObjectsOfType<Tile>();
            Debug.Log($"[PathfindingDebugTester] Found {tiles.Length} tiles. Hooking click events...");
            
            foreach (var tile in tiles)
            {
                // Create a closure to capture tile reference
                var tileRef = tile;
                // We'll handle this via Update() checking for tile clicks instead
            }
        }

        private void Update()
        {
            if (!_debugEnabled) return;

            // Check for mouse clicks
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                HandleTileClick();
            }
            else if (Input.GetMouseButtonDown(1)) // Right click
            {
                HandleReset();
            }
        }

        private void HandleTileClick()
        {
            // Ottieni la cella clickata
            var mousePos = Input.mousePosition;
            var worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            if (!_gridService.TryWorldToCell(worldPos, out Vector3Int cellInt))
            {
                Debug.LogWarning("[PathfindingDebugTester] Click fuori dalla griglia");
                return;
            }

            var cell = new Vector2Int(cellInt.x, cellInt.y);

            // Primo click = start
            if (_startCell == null)
            {
                _startCell = cell;
                Debug.Log($"[PathfindingDebugTester] ✓ START: {cell}");
                return;
            }

            // Secondo click = goal
            if (_goalCell == null)
            {
                _goalCell = cell;
                Debug.Log($"[PathfindingDebugTester] ✓ GOAL: {cell}");

                // Calcola e visualizza il percorso
                TestPathfinding(_startCell.Value, _goalCell.Value);
                return;
            }

            // Reset su terzo click
            HandleReset();
        }

        private void HandleReset()
        {
            _startCell = null;
            _goalCell = null;
            Debug.Log("[PathfindingDebugTester] ✓ Reset. Click first tile for new START");
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

            // Usa versione SYNC per testing (più facile da debuggare)
            var path = _pathfinding.FindPath(start, goal);

            // Visualizza il percorso
            _pathfinding.DebugVisualizePath(path);
            
            if (path.Count == 0)
            {
                Debug.LogWarning("[PathfindingDebugTester] ❌ No path found!");
            }
            else
            {
                Debug.Log($"[PathfindingDebugTester] ✓ Path length: {path.Count} cells");
            }
        }
    }
}
