using UnityEngine;

namespace Script.GridSystem
{
    public abstract class GridEvent
    {
        private Vector2Int position;

        public GridEvent(Vector2Int position)
        {
            this.position = position;
        }

        public Vector2Int GetPosition() { return position; }
    }
}