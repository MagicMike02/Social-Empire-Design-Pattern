using Script.BuildingSystem;
using UnityEngine;

namespace Script.Core.Events
{
    /// <summary>
    /// Pubblicato quando un edificio viene piazzato con successo sulla griglia.
    /// Publisher: BuildingPlacer / BuildingManager
    /// Subscribers: Grid (occupa celle), Audio, VFX, Achievements, Quests
    /// </summary>
    public readonly struct BuildingPlacedEvent
    {
        public readonly Building BuildingInstance;
        public readonly Vector3Int GridPosition;
        public readonly string BuildingName;

        public BuildingPlacedEvent(Building building, Vector3Int gridPosition, string buildingName)
        {
            BuildingInstance = building;
            GridPosition = gridPosition;
            BuildingName = buildingName;
        }
    }

    /// <summary>
    /// Pubblicato quando un edificio viene distrutto/rimosso.
    /// Publisher: BuildingManager
    /// Subscribers: Grid (libera celle), Resources (refund), UI, VFX
    /// </summary>
    public readonly struct BuildingDestroyedEvent
    {
        public readonly Building BuildingInstance;
        public readonly Vector3Int GridPosition;
        public readonly bool WasRefunded;

        public BuildingDestroyedEvent(Building building, Vector3Int gridPosition, bool wasRefunded)
        {
            BuildingInstance = building;
            GridPosition = gridPosition;
            WasRefunded = wasRefunded;
        }
    }

    /// <summary>
    /// Pubblicato quando un edificio viene selezionato dall'utente.
    /// Publisher: InputManager / BuildingPlacer
    /// Subscribers: UI (info panel), Camera (focus), Selection highlight
    /// </summary>
    public readonly struct BuildingSelectedEvent
    {
        public readonly Building BuildingInstance;

        public BuildingSelectedEvent(Building building)
        {
            BuildingInstance = building;
        }
    }
}
