using Script.EntitySystem.Building;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class BuildingSelectedEvent : GridEvent
    {
        private BuildingType buildingType;

        public BuildingSelectedEvent(BuildingType buildingType) : base(Vector2Int.zero) //posizione di default
        {
            this.buildingType = buildingType;
        }

        public BuildingType GetBuildingType() { return buildingType; }
    }
}