using UnityEngine;
using VContainer;
using Script2.BuildingSystem;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using System.Collections;
using Script2.GridSystem;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// PathfindingManager: Implements A* pathfinding algorithm on isometric grid.
    /// SPRINT 1 - Subtask 1.2: Synchronous A* implementation ✓
    /// SPRINT 1 - Subtask 1.3: Async execution via Unity Jobs ✓
    /// </summary>
    public class PathfindingManager : MonoBehaviour
    {
        [Inject] private IGridService _gridService;

        // Cache per evitare allocazioni ripetute
        private TileManager _tileManager; // Will be set via GridManager inspection

        /// <summary>
        /// Trova il percorso più breve tra start e goal usando A*.
        /// Versione SYNC (blocking) - sarà sostituita da async in Subtask 1.3.
        /// </summary>
        /// <param name="start">Cella di partenza</param>
        /// <param name="goal">Cella obiettivo</param>
        /// <returns>Lista di celle da percorrere (include start e goal), vuota se nessun percorso</returns>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            // Early exit: start == goal
            if (start == goal)
                return new List<Vector2Int> { start };

            // Early exit: goal not walkable
            if (!_gridService.IsCellWalkable(goal))
                return new List<Vector2Int>(); // No path

            // A* data structures
            var openSet = new PriorityQueue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };

            openSet.Enqueue(start, fScore[start]);

            while (openSet.Count > 0)
            {
                Vector2Int current = openSet.Dequeue();

                // Goal reached
                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                // Expand neighbors
                foreach (var neighbor in _gridService.GetWalkableNeighbors(current))
                {
                    float tentativeGScore = gScore[current] + 1f; // Uniform cost

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        // This path to neighbor is better
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);

                        // Add to open set if not already there
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                        }
                    }
                }
            }

            // No path found
            return new List<Vector2Int>();
        }

        /// <summary>
        /// Trova il percorso async usando Unity Jobs (NON blocca main thread).
        /// SUBTASK 1.3: Versione ASYNC - delegata a Job thread.
        /// </summary>
        /// <param name="start">Cella di partenza</param>
        /// <param name="goal">Cella obiettivo</param>
        /// <param name="callback">Callback invocato quando pathfinding completa (con risultato)</param>
        public void FindPathAsync(Vector2Int start, Vector2Int goal, System.Action<List<Vector2Int>> callback)
        {
            StartCoroutine(FindPathAsyncCoroutine(start, goal, callback));
        }

        private IEnumerator FindPathAsyncCoroutine(Vector2Int start, Vector2Int goal, System.Action<List<Vector2Int>> callback)
        {
            // Step 1: Genera walkability grid (main thread - accesso a Unity objects)
            var walkableGrid = GenerateWalkableGridNativeArray(out int width, out int height);

            if (!walkableGrid.IsCreated)
            {
                Debug.LogError("[PathfindingManager] Failed to generate walkable grid");
                callback?.Invoke(new List<Vector2Int>());
                yield break;
            }

            // Step 2: Crea Job e schedule (job thread execution)
            var resultPath = new NativeList<Vector2Int>(Allocator.TempJob);

            var job = new PathfindingJob
            {
                Start = start,
                Goal = goal,
                GridWidth = width,
                GridHeight = height,
                WalkableGrid = walkableGrid,
                ResultPath = resultPath
            };

            JobHandle handle = job.Schedule();

            // Step 3: Wait for job completion (yield return fa che Unity controlla ogni frame)
            while (!handle.IsCompleted)
            {
                yield return null; // Wait one frame
            }

            handle.Complete(); // Forza completion (se non già completo)

            // Step 4: Copia risultato a managed List e cleanup
            var path = new List<Vector2Int>(resultPath.Length);
            for (int i = 0; i < resultPath.Length; i++)
            {
                path.Add(resultPath[i]);
            }

            resultPath.Dispose();
            walkableGrid.Dispose();

            // Step 5: Invoke callback con risultato
            callback?.Invoke(path);
        }

        /// <summary>
        /// Genera NativeArray con walkability di ogni cella (per Unity Job).
        /// Accede a GridManager, quindi deve essere chiamato su main thread.
        /// </summary>
        private NativeArray<bool> GenerateWalkableGridNativeArray(out int width, out int height)
        {
            // TODO: Get grid size from GridManager (need reference)
            // For now, assume 50x50 (configurabile)
            width = 50;
            height = 50;

            var walkable = new NativeArray<bool>(width * height, Allocator.TempJob);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x; // Row-major order
                    walkable[index] = _gridService.IsCellWalkable(new Vector2Int(x, y));
                }
            }

            return walkable;
        }

        /// <summary>
        /// Heuristic per A*: Manhattan distance (isometric compatible)
        /// </summary>
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Ricostruisce il percorso dal dictionary cameFrom
        /// </summary>
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current); // Prepend
            }
            return path;
        }

        // ========== DEBUG VISUALIZATION ==========
        
        /// <summary>
        /// DEBUG ONLY: Visualizza il percorso calcolato colorando i tile.
        /// Mostra il path in blu, start in verde, goal in rosso.
        /// Disabilita questa chiamata in produzione.
        /// </summary>
        public void DebugVisualizePath(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                Debug.Log("[PathfindingManager] DEBUG: No path found");
                return;
            }

            Debug.Log($"[PathfindingManager] DEBUG: Path found with {path.Count} cells");

            // Color per tile sul percorso
            var pathColor = new Color(0f, 0.5f, 1f, 0.6f); // Blu semi-trasparente
            var startColor = new Color(0f, 1f, 0f, 0.8f);   // Verde
            var goalColor = new Color(1f, 0f, 0f, 0.8f);    // Rosso

            var grid = _tileManager?.GetGrid();
            if (grid == null) return;

            // Colora il percorso
            for (int i = 0; i < path.Count; i++)
            {
                var cell = path[i];
                var tile = grid.GetValue(cell.x, cell.y);
                if (tile == null) continue;

                if (i == 0)
                    tile.PreviewTint(startColor); // Start (verde)
                else if (i == path.Count - 1)
                    tile.PreviewTint(goalColor); // Goal (rosso)
                else
                    tile.PreviewTint(pathColor); // Path (blu)
            }
        }

        /// <summary>
        /// DEBUG ONLY: Pulisce la visualizzazione del percorso.
        /// </summary>
        public void DebugClearPathVisualization()
        {
            var grid = _tileManager?.GetGrid();
            if (grid == null) return;

            // Trova tutti i tile e resetta il tint
            // TODO: Traccia quali tile sono stati colorati per pulirli efficacemente
            // Per ora, una soluzione semplice: aspetta che Tile.ResetTint venga chiamato
        }
    }

    /// <summary>
    /// Simple priority queue for A* open set.
    /// Elements stored with priority (fScore), dequeued in ascending order.
    /// </summary>
    public class PriorityQueue<T>
    {
        private readonly List<(T item, float priority)> _elements = new();

        public int Count => _elements.Count;

        public void Enqueue(T item, float priority)
        {
            _elements.Add((item, priority));
        }

        public T Dequeue()
        {
            int bestIndex = 0;
            for (int i = 1; i < _elements.Count; i++)
            {
                if (_elements[i].priority < _elements[bestIndex].priority)
                    bestIndex = i;
            }

            T bestItem = _elements[bestIndex].item;
            _elements.RemoveAt(bestIndex);
            return bestItem;
        }

        public bool Contains(T item)
        {
            foreach (var element in _elements)
            {
                if (EqualityComparer<T>.Default.Equals(element.item, item))
                    return true;
            }
            return false;
        }
    }
}
