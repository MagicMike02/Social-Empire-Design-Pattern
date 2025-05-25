using Script.EntitySystem.Building;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class BuildingBuiltEvent : GridEvent
    {
        private BuildingType buildingType;

        public BuildingBuiltEvent(Vector2Int position, BuildingType buildingType) : base(position)
        {
            this.buildingType = buildingType;
        }

        public BuildingType GetBuildingType() { return buildingType; }
    }
}