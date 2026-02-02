using System.Collections.Generic;
using UnityEngine;

namespace Script.ResourceSystem.ResourceGenerationStrategy
{
    public class RegularGridGenerationStrategy : IResourceGenerationStrategy
    {
        public List<Vector2Int> GenerateResourcePositions(Vector2Int origin, int groupSize)
        {
            List<Vector2Int> pattern = new List<Vector2Int>();
        
            int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(groupSize));
            int gridHeight = Mathf.CeilToInt((float)groupSize / gridWidth);

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (pattern.Count < groupSize)
                    {
                        Vector2Int position = origin + new Vector2Int(x, y);
                        pattern.Add(position);
                    }
                }
            }

            return pattern;
        }
    }
}