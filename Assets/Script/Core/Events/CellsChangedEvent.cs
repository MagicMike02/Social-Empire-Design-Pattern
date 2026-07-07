using System.Collections.Generic;
using UnityEngine;

namespace Script.Core.Events
{
    /// <summary>
    /// Event emitted when one or more grid cells change occupancy state.
    /// Carries the list of affected cell coordinates.
    /// </summary>
    public readonly struct CellsChangedEvent
    {
        public readonly IReadOnlyList<Vector2Int> ChangedCells;

        public CellsChangedEvent(IReadOnlyList<Vector2Int> changedCells)
        {
            ChangedCells = changedCells;
        }
    }
}
