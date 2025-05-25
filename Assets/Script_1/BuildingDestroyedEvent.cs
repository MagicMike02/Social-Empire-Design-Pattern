using Script.EntitySystem.Building;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class BuildingDestroyedEvent : GridEvent
    {
        private BuildingType buildingType;

        public BuildingDestroyedEvent(Vector2Int position, BuildingType buildingType) : base(position)
        {
            this.buildingType = buildingType;
        }

        public BuildingType GetBuildingType() { return buildingType; }
    }
}