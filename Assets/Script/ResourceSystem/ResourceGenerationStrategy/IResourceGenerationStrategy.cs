using System.Collections.Generic;
using UnityEngine;

namespace Script.ResourceSystem.ResourceGenerationStrategy
{
    public interface IResourceGenerationStrategy
    {
        List<Vector2Int> GenerateResourcePositions(Vector2Int origin, int groupSize);
    }
}