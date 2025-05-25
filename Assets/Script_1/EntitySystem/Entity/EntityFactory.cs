using System;

namespace Script.EntitySystem.Entity
{
    using System.Collections.Generic;
    using Building;
    using Resource;
    using Unit;
    using ScriptableObject;
    using UnityEngine;


    public class EntityFactory : IEntityFactory  
    {
        private Dictionary<string, ScriptableObject> dataObjects = new Dictionary<string, ScriptableObject>();
        
        public void LoadDataObjects()
        {
            // Carica gli ScriptableObject dalle cartelle delle risorse.
            LoadUnitData();
            LoadBuildingData();
            LoadResourceData();
            // ... Altre cartelle
        }

        private void LoadUnitData()
        {
            UnitDataSO[] unitDataSOs = Resources.LoadAll<UnitDataSO>("Data/Units"); //assicurati che la cartella esista
            foreach (UnitDataSO dataSO in unitDataSOs)
            {
                dataObjects.Add(dataSO.name, dataSO);
            }
        }

        private void LoadBuildingData()
        {
            BuildingDataSO[] buildingDataSOs = Resources.LoadAll<BuildingDataSO>("Data/Buildings"); //assicurati che la cartella esista
            foreach (BuildingDataSO dataSO in buildingDataSOs)
            {
                dataObjects.Add(dataSO.name, dataSO);
            }
        }

        private void LoadResourceData()
        {
            ResourceDataSO[] resourceDataSOs = Resources.LoadAll<ResourceDataSO>("Data/Resources"); //assicurati che la cartella esista
            foreach (ResourceDataSO dataSO in resourceDataSOs)
            {
                dataObjects.Add(dataSO.name, dataSO);
            }
        }


        public IEntity CreateEntity(string entityType, Vector2Int position)
        {
            if (!dataObjects.ContainsKey(entityType))
            {
                Debug.LogError("Entity type not found: " + entityType);
                return null;
            }

            // Creazione di istanze basate sul tipo di ScriptableObject.
            if (dataObjects[entityType] is UnitDataSO unitDataSO)
            {
                // Unit newUnit = new Unit(position, unitDataSO);
                IEntity newUnit = new Unit(position, unitDataSO);
                return newUnit;
            }
            else if (dataObjects[entityType] is BuildingDataSO buildingDataSO)
            {
                Building newBuilding = new Building(position, buildingDataSO);
                return newBuilding;
            }
            else if (dataObjects[entityType] is ResourceDataSO resourceDataSO)
            {
                Resource newResource = new Resource(position, resourceDataSO);
                return newResource;
            }
            else
            {
                Debug.LogError("Unsupported entity type: " + entityType);
                return null;
            }
        }
    }
}