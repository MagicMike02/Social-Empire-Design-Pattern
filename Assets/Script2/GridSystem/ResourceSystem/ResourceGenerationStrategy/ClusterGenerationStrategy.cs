using System.Collections.Generic;
using UnityEngine;

namespace Script2.GridSystem.ResourceSystem.ResourceGenerationStrategy
{
    public class ClusterGenerationStrategy : IResourceGenerationStrategy
    {
        public List<Vector2Int> GenerateResourcePositions(Vector2Int origin, int groupSize)
        {
            List<Vector2Int> pattern = new List<Vector2Int> { origin };
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            int currentIndex = 0;
            while (pattern.Count < groupSize && currentIndex < pattern.Count)
            {
                Vector2Int basePos = pattern[currentIndex];
                Vector2Int dir = directions[Random.Range(0, directions.Length)];
                Vector2Int newPos = basePos + dir;

                if (!pattern.Contains(newPos))
                {
                    pattern.Add(newPos);
                }

                currentIndex++;
            }

            return pattern;
        }
    }

}