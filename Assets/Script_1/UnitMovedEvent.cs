using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class UnitMovedEvent : GridEvent
    {
        private Vector2Int oldPosition;

        public UnitMovedEvent(Vector2Int position, Vector2Int oldPosition) : base(position)
        {
            this.oldPosition = oldPosition;
        }

        public Vector2Int GetOldPosition() { return oldPosition; }
    }
}