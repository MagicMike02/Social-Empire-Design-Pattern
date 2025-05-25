using System;
using Script.ScriptableObject;
using UnityEngine;

namespace Script.EntitySystem.Building
{
    public class Building : Entity.Entity
    {
        public BuildingDataSO BuildingDataSO { get; private set; }
        private BuildingInstanceData instanceData;
        public BuildingType buildingType;

        public Building(Vector2Int position, BuildingDataSO buildingDataSO)
        {
            this.BuildingDataSO = buildingDataSO;
            this.instanceData = new BuildingInstanceData
            {
                id = new Guid(),
                health = buildingDataSO.maxHealth,
                position = position,
            };
            this.buildingType = buildingDataSO.buildingType;
        }

        public string GetBuildingName()
        {
            return BuildingDataSO.buildingName;
        }

        public override Guid GetId()
        {
            throw new NotImplementedException();
        }

        public override int GetHealth()
        {
            return instanceData.health;
        }

        public override void SetHealth(int newHealth)
        {
            instanceData.health = newHealth;
        }

        public int GetWidth()
        {
            return BuildingDataSO.width;
        }

        public int GetHeight()
        {
            return BuildingDataSO.height;
        }

        public int GetMaxHealth()
        {
            return BuildingDataSO.maxHealth;
        }

        protected virtual void Die()
        {
            // Logica di morte dell'entità (es. Distruzione, animazione)
            Destroy(gameObject);
        }
        
        public override Vector2Int GetPosition()
        {
            return instanceData.position;
        }

        public override void SetPosition(Vector2Int newPosition)
        {
            throw new NotImplementedException();
        }

        public override int GetZorder()
        {
            return 0;
        } // Gli edifici sono sopra le unità.

        public override void ExecuteCommand(ICommand command)
        {
            // Implementazione specifica per gli edifici.
            Debug.Log("Building ExecuteCommand: " + command.GetType().Name);
        }
    }
}