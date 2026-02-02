using System.Collections.Generic;
using Script.Core.Events;
using UnityEngine;
using VContainer;

namespace Script.PathfindingSystem 
{
    /// <summary>
    /// Test script per verificare PathfindingManager integration.
    /// USAGE: Attacca a GameObject, premi tasti:
    /// - F: FindPath tra punti configurati (colora celle)
    /// - C: Clear path visualization
    /// - P: Simula grid change (cache invalidation test)
    ///
    /// NOTA: Visualizzazione path colorata via DebugPathfindingDecorator
    /// Attiva il toggle "Enable Debug Visualization" su PathfindingManager in Inspector
    /// </summary>
    public class PathfindingTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private Vector2Int _startCell = new Vector2Int(40, 59);
        [SerializeField] private Vector2Int _goalCell = new Vector2Int(59, 40);
        
        [Header("Dependencies (Auto-Injected)")]
        private PathfindingManager _pathfindingManager;

        private List<Vector2Int> _lastPath = new();

        [Inject]
        public void Construct(PathfindingManager pathfindingManager)
        {
            _pathfindingManager = pathfindingManager;
        }

        private void Update()
        {
            // F: Find Path
            if (Input.GetKeyDown(KeyCode.F))
            {
                TestFindPath();
            }

            // P: Simulate grid change (pubblica evento test)
            if (Input.GetKeyDown(KeyCode.P))
            {
                SimulateGridChange();
            }
        }

        private void TestFindPath()
        {
            if (_pathfindingManager == null)
            {
                Debug.LogError("[PathfindingTester] PathfindingManager non iniettato!");
                return;
            }

            Debug.Log($"[PathfindingTester] === TESTING PATH: {_startCell} → {_goalCell} ===");

            var path = _pathfindingManager.FindPath(_startCell, _goalCell);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[PathfindingTester] ❌ NO PATH FOUND between {_startCell} and {_goalCell}");
            }
            else
            {
                Debug.Log($"[PathfindingTester] ✅ PATH FOUND: {path.Count} cells");
                _lastPath = new List<Vector2Int>(path);
            }
        }

        private void SimulateGridChange()
        {
            var testOrigin = new Vector3Int(50, 50, 1);
            Debug.Log($"[PathfindingTester] 🏗️ Simulating building placement at {testOrigin} (2x2)");
            
            GlobalEventBus.Publish(new CellsOccupiedEvent(testOrigin, 2, 2, null));
            
            Debug.Log("[PathfindingTester] Premi 'F' per verificare ricalcolo path (cache invalidato).");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 180));
            GUILayout.Label("=== PATHFINDING TESTER ===");
            GUILayout.Label($"Start: {_startCell} | Goal: {_goalCell}");
            GUILayout.Label($"Last path: {_lastPath.Count} cells");
            GUILayout.Label("");
            GUILayout.Label("CONTROLS:");
            GUILayout.Label("  F = Find Path");
            GUILayout.Label("  P = Simulate Grid Change");
            GUILayout.Label("");
            GUILayout.Label("VISUALIZATION:");
            GUILayout.Label("  Enable 'Debug Visualization' toggle on");
            GUILayout.Label("  PathfindingManager in Inspector to see colored path:");
            GUILayout.Label("  - Green = Start");
            GUILayout.Label("  - Blue = Path");
            GUILayout.Label("  - Red = Goal");
            GUILayout.EndArea();
        }

        #region Editor Helpers

        [ContextMenu("Find Path")]
        private void EditorFindPath()
        {
            if (Application.isPlaying)
                TestFindPath();
        }
        
        
        [ContextMenu("Simulate Grid Change")]
        private void EditorCleanPath()
        {
            if (Application.isPlaying)
            {
                
            }
               
        }

        [ContextMenu("Simulate Grid Change")]
        private void EditorGridChange()
        {
            if (Application.isPlaying)
                SimulateGridChange();
        }

        #endregion
    }
}

