using System.Collections.Generic;
using Script.BuildingSystem;
using UnityEngine;

namespace Script.PathfindingSystem
{
    /// <summary>
    /// A* Pathfinding Algorithm - Concrete Strategy implementation.
    /// 8-directional movement con costi accurati (cardinal=1, diagonal=sqrt(2)).
    /// </summary>
    public class AStarAlgorithm : IPathfindingAlgorithm
    {
        #region Public APIs

        /// <summary>
        /// Trova il percorso ottimale tramite A* (8-directional).
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, IGridService gridService)
        {
            // Early exit: start == goal
            if (start == goal)
                return new List<Vector2Int> { start };

            // Early exit: goal not walkable
            if (!gridService.IsCellWalkable(goal))
                return new List<Vector2Int>();

            // A* data structures - OPTIMIZED with BinaryHeap
            var openSet = new BinaryHeapPriorityQueue<Vector2Int>();
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
                foreach (var neighbor in gridService.GetWalkableNeighbors(current))
                {
                    // Calculate cost based on direction (cardinal=1, diagonal=sqrt(2)≈1.414)
                    float movementCost = Mathf.Abs(neighbor.x - current.x) + Mathf.Abs(neighbor.y - current.y) == 2
                        ? 1.414f  // Diagonal movement
                        : 1f;     // Cardinal movement

                    float tentativeGScore = gScore[current] + movementCost;

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        // This path to neighbor is better
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        float newFScore = gScore[neighbor] + Heuristic(neighbor, goal);
                        fScore[neighbor] = newFScore;

                        // Use UpdatePriority instead of re-enqueue
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
            return new List<Vector2Int>();
        }
        
        #endregion

        #region Internal Helpers

        /// <summary>
        /// Heuristic per A*: Chebyshev distance (optimal per 8-directional movement)
        /// </summary>
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        /// <summary>
        /// OPTIMIZED: Ricostruisce il percorso O(n).
        /// </summary>
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int>();
            var node = current;
            
            while (cameFrom.ContainsKey(node))
            {
                path.Add(node);
                node = cameFrom[node];
            }
            path.Add(node);
            
            path.Reverse();
            return path;
        }
        
        #endregion
    }
}
