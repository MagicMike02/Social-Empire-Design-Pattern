using UnityEngine;

namespace Script.Core.Events
{
    /// <summary>
    /// Pubblicato quando un percorso viene trovato con successo.
    /// Publisher: PathfindingManager
    /// Subscribers: Debug visualization, Performance monitoring
    /// </summary>
    public readonly struct PathFoundEvent
    {
        public readonly Vector2Int Start;
        public readonly Vector2Int Goal;
        public readonly int PathLength;
        public readonly float ComputationTimeMs;

        public PathFoundEvent(Vector2Int start, Vector2Int goal, int pathLength, float computationTimeMs)
        {
            Start = start;
            Goal = goal;
            PathLength = pathLength;
            ComputationTimeMs = computationTimeMs;
        }
    }
}
