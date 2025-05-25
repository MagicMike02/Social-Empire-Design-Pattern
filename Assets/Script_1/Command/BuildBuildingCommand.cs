using Script.EntitySystem.Building;
using UnityEngine;

namespace Script.Command
{
    public class BuildBuildingCommand : ICommand{
        private BuildingType buildingType;
        private Vector2Int buildPosition;

        public BuildBuildingCommand(BuildingType buildingType, Vector2Int buildPosition){
            this.buildingType = buildingType;
            this.buildPosition = buildPosition;
        }

        public bool CanExecute(IWorld world){
            //controlla se si puo costruire
            return world.CanBuild(null, buildPosition); //null perche non abbiamo l'istanzadi building
        }

        public void Execute(IWorld world){
            if(CanExecute(world)){
                world.BuildBuilding(buildingType, buildPosition);
            }
            else{
                Debug.LogWarning("Cannot execute BuildBuildingCommand: Cannot build at the position.");
            }
        }
    }
}