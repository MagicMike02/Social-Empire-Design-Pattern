using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Interface for objects that occupy grid positions (Tiles, Buildings, Units, etc).
    /// Provides direct access to grid position without requiring mathematical conversions.
    /// Source of Truth for grid-based positioning.
    /// </summary>
    public interface IGridEntity
    {
        /// <summary>
        /// The grid position of this entity (cached at initialization, never computed).
        /// This is the authoritative source for determining which cell an entity occupies.
        /// </summary>
        Vector2Int GridPosition { get; }
    }
}
