using UnityEngine;
using Script2.PathfindingSystem;
using Script2.Core.Events;
using VContainer;

namespace Script2.Testing
{
    /// <summary>
    /// Test script per verificare PathfindingManager integration.
    /// USAGE: Attacca a GameObject, assegna riferimenti in Inspector, premi tasti:
    /// - F: FindPath tra punti configurati
    /// - C: Clear cache manualmente
    /// - P: Piazza edificio test (simula occupazione celle)
    /// </summary>
    public class PathfindingTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private Vector2Int _startCell = new Vector2Int(5, 5);
        [SerializeField] private Vector2Int _goalCell = new Vector2Int(15, 15);
        
        [Header("Dependencies (Auto-Injected)")]
        private PathfindingManager _pathfindingManager;

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

            // C: Clear cache manually
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("[PathfindingTester] Manual cache clear requested (eventi lo fanno automaticamente)");
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
                Debug.LogWarning("Possibili cause: celle bloccate da edifici/risorse, start/goal non walkable");
            }
            else
            {
                Debug.Log($"[PathfindingTester] ✅ PATH FOUND: {path.Count} cells");
                Debug.Log($"[PathfindingTester] Path: {string.Join(" → ", path)}");
            }
        }

        private void SimulateGridChange()
        {
            // Simula evento occupazione celle (come se piazzassimo edificio)
            var testOrigin = new Vector3Int(10, 10, 0);
            Debug.Log($"[PathfindingTester] 🏗️ Simulating building placement at {testOrigin} (2x2)");
            
            GlobalEventBus.Publish(new CellsOccupiedEvent(testOrigin, 2, 2, null));
            
            Debug.Log("[PathfindingTester] → Evento pubblicato. PathfindingManager dovrebbe aver invalidato cache.");
            Debug.Log("[PathfindingTester] Premi 'F' per verificare che path venga ricalcolato.");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.Label("=== PATHFINDING TESTER ===");
            GUILayout.Label($"Start: {_startCell} | Goal: {_goalCell}");
            GUILayout.Label("F = Find Path");
            GUILayout.Label("P = Simulate Grid Change");
            GUILayout.Label("C = Clear Cache (manual)");
            GUILayout.EndArea();
        }
    }
}
