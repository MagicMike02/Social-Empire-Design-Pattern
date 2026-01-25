using UnityEngine;
using VContainer;
using Script2.PathfindingSystem;
using Script2.BuildingSystem;
using Script2.InputSystem;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// DEBUG TEST SCRIPT: Testare pathfinding senza Units.
    /// Click su primo tile = start
    /// Click su secondo tile = goal
    /// Visualizza il percorso colorando i tile (blu = path, verde = start, rosso = goal)
    /// </summary>
    public class PathfindingDebugTester : MonoBehaviour, IHoverable
    {
        [Inject] private IGridService _gridService;
        [Inject] private PathfindingManager _pathfinding;

        private Vector2Int? _startCell = null;
        private Vector2Int? _goalCell = null;

        [SerializeField] private bool _debugEnabled = true;

        private void Start()
        {
            if (_debugEnabled)
                Debug.Log("[PathfindingDebugTester] Ready. Click first tile for START, second tile for GOAL");
        }

        // Implementazione IHoverable (per ricevere click da InputManager)
        public void OnHoverEnter()
        {
            // No visual feedback needed for debug
        }

        public void OnHoverExit()
        {
            // No cleanup needed
        }

        public void OnClick()
        {
            if (!_debugEnabled) return;

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
                Debug.Log($"[PathfindingDebugTester] START set: {cell}");
                return;
            }

            // Secondo click = goal
            if (_goalCell == null)
            {
                _goalCell = cell;
                Debug.Log($"[PathfindingDebugTester] GOAL set: {cell}");

                // Calcola e visualizza il percorso
                TestPathfinding(_startCell.Value, _goalCell.Value);
                return;
            }

            // Reset su terzo click
            _startCell = null;
            _goalCell = null;
            Debug.Log("[PathfindingDebugTester] Reset. Click again for new START");
        }

        public void OnRightClick(Vector3 worldPosition)
        {
            // Right click = reset
            _startCell = null;
            _goalCell = null;
            Debug.Log("[PathfindingDebugTester] Reset via right-click");
        }

        private void TestPathfinding(Vector2Int start, Vector2Int goal)
        {
            Debug.Log($"[PathfindingDebugTester] Testing pathfinding: {start} → {goal}");

            // Usa versione SYNC per testing (più facile da debuggare)
            var path = _pathfinding.FindPath(start, goal);

            // Visualizza il percorso
            _pathfinding.DebugVisualizePath(path);
        }
    }
}
