using System.Collections.Generic;
using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script.PathFinding
{
    public interface IPathfindingAlgorithm
    {
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, List<Unit> group);
    }
}