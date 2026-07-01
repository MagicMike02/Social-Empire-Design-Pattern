using UnityEngine;

namespace Script.Core.Events
{
    /// <summary>
    /// Pubblicato quando l'utente clicca su un tile della griglia.
    /// Publisher: InputManager
    /// Subscribers: BuildingPlacer (placement), UnitCommands (move to), Selection
    /// </summary>
    public readonly struct TileClickedEvent
    {
        public readonly Vector2Int GridPosition;
        public readonly Vector3 WorldPosition;

        public TileClickedEvent(Vector2Int gridPosition, Vector3 worldPosition)
        {
            GridPosition = gridPosition;
            WorldPosition = worldPosition;
        }
    }
}
