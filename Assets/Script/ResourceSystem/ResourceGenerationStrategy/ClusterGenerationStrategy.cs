using System.Collections.Generic;
using UnityEngine;

namespace Script.ResourceSystem.ResourceGenerationStrategy
{
    public class ClusterGenerationStrategy : IResourceGenerationStrategy
    {
        //  avoid allocating it on every call.
        private static readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public List<Vector2Int> GenerateResourcePositions(Vector2Int origin, int groupSize)
        {
            // Handle edge case for groupSize <= 0
            if (groupSize <= 0)
            {
                return new List<Vector2Int>();
            }

            // Pre-allocating the list with the expected size can avoid reallocations.
            var pattern = new List<Vector2Int>(groupSize) { origin };

            // Using a HashSet for checking existence is much faster (O(1) on average) 
            var patternSet = new HashSet<Vector2Int> { origin };

            int currentIndex = 0;
            while (pattern.Count < groupSize && currentIndex < pattern.Count)
            {
                Vector2Int basePos = pattern[currentIndex];
                Vector2Int dir = directions[Random.Range(0, directions.Length)];
                Vector2Int newPos = basePos + dir;

                // HashSet.Add returns true if the item was added, false if it already existed.
                if (patternSet.Add(newPos))
                {
                    pattern.Add(newPos);
                }

                currentIndex++;
            }

            return pattern;
        }
    }


}