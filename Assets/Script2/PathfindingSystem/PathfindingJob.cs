using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Script2.PathfindingSystem
{
    /// <summary>
    /// Unity Job per pathfinding A* async.
    /// SPRINT 1 - Subtask 1.3: Offload A* execution to separate thread.
    /// 
    /// COME FUNZIONA:
    /// 1. Main thread: crea job, passa walkability grid (NativeArray), schedule
    /// 2. Job thread: esegue A* algorithm su griglia
    /// 3. Main thread: riceve risultato via callback quando job completa
    /// 
    /// PERFORMANCE TARGET: less than 5ms per path su 100x100 grid
    /// </summary>
    public struct PathfindingJob : IJob
    {
        // Input (read-only)
        public Vector2Int Start;
        public Vector2Int Goal;
        public int GridWidth;
        public int GridHeight;
        
        [ReadOnly] public NativeArray<bool> WalkableGrid; // Flattened 2D grid (row-major order)

        // Output (write-only)
        public NativeList<Vector2Int> ResultPath;

        public void Execute()
        {
            // Early exit: start == goal
            if (Start.Equals(Goal))
            {
                ResultPath.Add(Start);
                return;
            }

            // Early exit: goal not walkable
            if (!IsWalkable(Goal))
            {
                // Empty result path = no path found
                return;
            }

            // A* implementation (same logic as sync version, but with Native collections)
            var openSet = new NativeList<PathNode>(Allocator.Temp);
            var closedSet = new NativeHashSet<Vector2Int>(GridWidth * GridHeight / 4, Allocator.Temp);
            var cameFrom = new NativeHashMap<Vector2Int, Vector2Int>(GridWidth * GridHeight / 4, Allocator.Temp);

            openSet.Add(new PathNode { Cell = Start, GScore = 0, FScore = Heuristic(Start, Goal) });

            while (openSet.Length > 0)
            {
                // Find node with lowest fScore (priority queue behavior)
                int bestIndex = 0;
                for (int i = 1; i < openSet.Length; i++)
                {
                    if (openSet[i].FScore < openSet[bestIndex].FScore)
                        bestIndex = i;
                }

                PathNode current = openSet[bestIndex];
                openSet.RemoveAtSwapBack(bestIndex);

                // Goal reached
                if (current.Cell.Equals(Goal))
                {
                    ReconstructPath(cameFrom, current.Cell);
                    closedSet.Dispose();
                    cameFrom.Dispose();
                    openSet.Dispose();
                    return;
                }

                closedSet.Add(current.Cell);

                // Expand neighbors (4-directional)
                var neighbors = GetWalkableNeighbors(current.Cell);
                for (int i = 0; i < neighbors.Length; i++)
                {
                    Vector2Int neighbor = neighbors[i];

                    if (closedSet.Contains(neighbor))
                        continue;

                    float tentativeGScore = current.GScore + 1f; // Uniform cost

                    // Check if neighbor already in open set
                    int neighborIndex = -1;
                    for (int j = 0; j < openSet.Length; j++)
                    {
                        if (openSet[j].Cell.Equals(neighbor))
                        {
                            neighborIndex = j;
                            break;
                        }
                    }

                    if (neighborIndex == -1 || tentativeGScore < openSet[neighborIndex].GScore)
                    {
                        // Better path to neighbor
                        cameFrom[neighbor] = current.Cell;
                        var newNode = new PathNode
                        {
                            Cell = neighbor,
                            GScore = tentativeGScore,
                            FScore = tentativeGScore + Heuristic(neighbor, Goal)
                        };

                        if (neighborIndex == -1)
                        {
                            openSet.Add(newNode);
                        }
                        else
                        {
                            openSet[neighborIndex] = newNode;
                        }
                    }
                }
            }

            // No path found (ResultPath remains empty)
            closedSet.Dispose();
            cameFrom.Dispose();
            openSet.Dispose();
        }

        private bool IsWalkable(Vector2Int cell)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= GridWidth || cell.y >= GridHeight)
                return false;

            int index = cell.y * GridWidth + cell.x; // Row-major order
            return WalkableGrid[index];
        }

        private NativeArray<Vector2Int> GetWalkableNeighbors(Vector2Int cell)
        {
            var neighbors = new NativeList<Vector2Int>(4, Allocator.Temp);

            // 4-directional (cross pattern for isometric)
            var candidates = new Vector2Int[4]
            {
                new Vector2Int(cell.x + 1, cell.y),  // East
                new Vector2Int(cell.x - 1, cell.y),  // West
                new Vector2Int(cell.x, cell.y + 1),  // North
                new Vector2Int(cell.x, cell.y - 1)   // South
            };

            foreach (var candidate in candidates)
            {
                if (IsWalkable(candidate))
                    neighbors.Add(candidate);
            }

            var result = neighbors.AsArray();
            neighbors.Dispose();
            return result;
        }

        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private void ReconstructPath(NativeHashMap<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            // Build path in reverse (from goal to start), then reverse it
            var reversePath = new NativeList<Vector2Int>(Allocator.Temp);
            reversePath.Add(current);
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                reversePath.Add(current);
            }

            // Add to ResultPath in correct order (reversed)
            for (int i = reversePath.Length - 1; i >= 0; i--)
            {
                ResultPath.Add(reversePath[i]);
            }
            
            reversePath.Dispose();
        }

        private struct PathNode
        {
            public Vector2Int Cell;
            public float GScore;
            public float FScore;
        }
    }
}
