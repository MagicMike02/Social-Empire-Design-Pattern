﻿﻿using UnityEngine;
using VContainer;
using Script2.BuildingSystem;
using Script2.GridSystem;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using System.Collections;
using System.Linq;

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
        
        // OPTIMIZATION: Path result cache (avoid recomputing same paths)
        private readonly Dictionary<(Vector2Int, Vector2Int), List<Vector2Int>> _pathCache = new();
        private const int MaxCacheSize = 100; // LRU limit

        private void Start()
        {
            // Cache TileManager per debug visualization
            _tileManager = FindFirstObjectByType<TileManager>();
        }

        /// <summary>
        /// Trova il percorso più breve tra start e goal usando A*.
        /// OPTIMIZED: Binary Heap O(log n), path caching, early exits.
        /// </summary>
        /// <param name="start">Cella di partenza</param>
        /// <param name="goal">Cella obiettivo</param>
        /// <returns>Lista di celle da percorrere (include start e goal), vuota se nessun percorso</returns>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            #if UNITY_EDITOR
            var sw = System.Diagnostics.Stopwatch.StartNew();
            #endif

            // Early exit: start == goal
            if (start == goal)
                return new List<Vector2Int> { start };

            // OPTIMIZATION: Check cache
            var cacheKey = (start, goal);
            if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
            {
                #if UNITY_EDITOR
                sw.Stop();
                Debug.Log($"[PathfindingManager] Cache HIT: {sw.ElapsedMilliseconds}ms");
                #endif
                return new List<Vector2Int>(cachedPath); // Return copy to avoid external mutation
            }

            // Early exit: goal not walkable
            if (!_gridService.IsCellWalkable(goal))
            {
                #if UNITY_EDITOR
                sw.Stop();
                Debug.Log($"[PathfindingManager] Goal not walkable: {sw.ElapsedMilliseconds}ms");
                #endif
                return new List<Vector2Int>(); // No path
            }

            // A* data structures - OPTIMIZED with BinaryHeap
            var openSet = new BinaryHeapPriorityQueue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };

            openSet.Enqueue(start, fScore[start]);

            int iterations = 0;

            while (openSet.Count > 0)
            {
                iterations++;
                Vector2Int current = openSet.Dequeue();

                // Goal reached
                if (current == goal)
                {
                    var path = ReconstructPathOptimized(cameFrom, current);
                    
                    // OPTIMIZATION: Cache result
                    if (_pathCache.Count >= MaxCacheSize)
                    {
                        // Simple LRU: rimuovi primo elemento
                        var firstKey = _pathCache.Keys.First();
                        _pathCache.Remove(firstKey);
                    }
                    _pathCache[cacheKey] = new List<Vector2Int>(path);
                    
                    #if UNITY_EDITOR
                    sw.Stop();
                    Debug.Log($"[PathfindingManager] Path found: {path.Count} cells, {iterations} iterations, {sw.ElapsedMilliseconds}ms");
                    #endif
                    
                    return path;
                }

                // Expand neighbors
                foreach (var neighbor in _gridService.GetWalkableNeighbors(current))
                {
                    float tentativeGScore = gScore[current] + 1f; // Uniform cost

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        // This path to neighbor is better
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        float newFScore = gScore[neighbor] + Heuristic(neighbor, goal);
                        fScore[neighbor] = newFScore;

                        // OPTIMIZATION: Use UpdatePriority instead of re-enqueue
                        if (openSet.Contains(neighbor))
                        {
                            openSet.UpdatePriority(neighbor, newFScore);
                        }
                        else
                        {
                            openSet.Enqueue(neighbor, newFScore);
                        }
                    }
                }
            }

            // No path found
            #if UNITY_EDITOR
            sw.Stop();
            Debug.LogWarning($"[PathfindingManager] NO PATH FOUND: {iterations} iterations, {sw.ElapsedMilliseconds}ms");
            #endif
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
            path.AddRange(resultPath);

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
            width = _gridService.Width;
            height = _gridService.Height;

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
        /// OPTIMIZED: Ricostruisce il percorso O(n) senza Insert inefficiente.
        /// Usa List.Add (O(1)) invece di List.Insert(0, ...) (O(n)).
        /// </summary>
        private List<Vector2Int> ReconstructPathOptimized(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int>();
            var node = current;
            
            // Build path backwards (O(n))
            while (cameFrom.ContainsKey(node))
            {
                path.Add(node);
                node = cameFrom[node];
            }
            path.Add(node); // Add start
            
            path.Reverse(); // Reverse once at end - O(n)
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

            // DEBUG: Trova TileManager in scena
            var tileManager = FindFirstObjectByType<TileManager>();
            if (tileManager == null)
            {
                Debug.LogError("[PathfindingManager] DEBUG: TileManager not found in scene!");
                return;
            }

            var grid = tileManager.GetGrid();
            if (grid == null)
            {
                Debug.LogError("[PathfindingManager] DEBUG: Grid is null!");
                return;
            }

            // Color per tile sul percorso
            var pathColor = new Color(0f, 0.5f, 1f, 0.6f); // Blu semi-trasparente
            var startColor = new Color(0f, 1f, 0f, 0.8f);   // Verde
            var goalColor = new Color(1f, 0f, 0f, 0.8f);    // Rosso

            // Colora il percorso
            for (int i = 0; i < path.Count; i++)
            {
                var cell = path[i];
                var tile = grid.GetValue(cell.x, cell.y);
                if (tile == null)
                {
                    Debug.LogWarning($"[PathfindingManager] DEBUG: Tile at {cell} is null!");
                    continue;
                }

                if (i == 0)
                {
                    tile.DebugSetColor(startColor); // Start (verde)
                    Debug.Log($"[PathfindingManager] DEBUG: START tile {cell} colored GREEN");
                }
                else if (i == path.Count - 1)
                {
                    tile.DebugSetColor(goalColor); // Goal (rosso)
                    Debug.Log($"[PathfindingManager] DEBUG: GOAL tile {cell} colored RED");
                }
                else
                {
                    tile.DebugSetColor(pathColor); // Path (blu)
                }
            }

            Debug.Log($"[PathfindingManager] DEBUG: ✓ Colored {path.Count} tiles");
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
}
