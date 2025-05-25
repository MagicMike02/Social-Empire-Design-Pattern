using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class CellChangedEvent : GridEvent
    {
        public CellChangedEvent(Vector2Int position) : base(position) { }
    }
}