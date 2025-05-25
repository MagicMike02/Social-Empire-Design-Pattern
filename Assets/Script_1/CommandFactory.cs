using System.Collections.Generic;
using Script.Command;
using Script.EntitySystem.Building;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Resource;
using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script
{
    public class CommandFactory : ICommandFactory
    {
        
        // Metodo per creare un comando in base al tipo e ai dati forniti.
        public ICommand CreateCommand(string commandType, Dictionary<string, object> commandData)
        {
            switch (commandType)
            {
                case "MoveUnitCommand":
                    if (commandData.ContainsKey("unit") && commandData.ContainsKey("targetPosition") &&
                        commandData.ContainsKey("group"))
                    {
                        Unit unit = (Unit)commandData["unit"];
                        Vector2Int targetPosition = (Vector2Int)commandData["targetPosition"];
                        List<Unit> group = (List<Unit>)commandData["group"];
                        return new MoveUnitCommand(unit, targetPosition, group);
                    }
                    else
                    {
                        throw new System.ArgumentException("Missing data for MoveUnitCommand.");
                    }
                case "AttackUnitCommand":
                    if (commandData.ContainsKey("attacker") && commandData.ContainsKey("target"))
                    {
                        Unit attacker = (Unit)commandData["attacker"];
                        IEntity target = (IEntity)commandData["target"];
                        return new AttackUnitCommand(attacker, target);
                    }
                    else
                    {
                        throw new System.ArgumentException("Missing data for AttackUnitCommand.");
                    }
                case "BuildBuildingCommand":
                    if (commandData.ContainsKey("buildingType") && commandData.ContainsKey("buildPosition"))
                    {
                        BuildingType buildingType = (BuildingType)commandData["buildingType"];
                        Vector2Int buildPosition = (Vector2Int)commandData["buildPosition"];
                        return new BuildBuildingCommand(buildingType, buildPosition);
                    }
                    else
                    {
                        throw new System.ArgumentException("Missing data for BuildBuildingCommand.");
                    }
                case "CollectResourceCommand":
                    if (commandData.ContainsKey("collector") && commandData.ContainsKey("resourceType"))
                    {
                        Unit collector = (Unit)commandData["collector"];
                        ResourceType resourceType = (ResourceType)commandData["resourceType"];
                        return new CollectResourceCommand(collector, resourceType);
                    }
                    else
                    {
                        throw new System.ArgumentException("Missing data for CollectResourceCommand.");
                    }
                default:
                    throw new System.ArgumentException("Invalid command type: " + commandType);
            }
        }
    }
}