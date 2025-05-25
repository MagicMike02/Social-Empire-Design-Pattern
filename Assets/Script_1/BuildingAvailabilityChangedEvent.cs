using Script.EntitySystem.Building;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class BuildingAvailabilityChangedEvent : GridEvent
    {
        private BuildingType buildingType;
        private bool isAvailable;

        public BuildingAvailabilityChangedEvent(BuildingType buildingType, bool isAvailable) : base(Vector2Int.zero)
        {
            this.buildingType = buildingType;
            this.isAvailable = isAvailable;
        }

        public BuildingType GetBuildingType() { return buildingType; }
        public bool IsAvailable() { return isAvailable; }
    }

}