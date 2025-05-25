using System;
using Script.ScriptableObject;
using UnityEngine;

namespace Script.EntitySystem.Resource
{
    public class Resource : Entity.Entity
    {
        public ResourceDataSO ResourceDataSO { get; private set; }
        private ResourceInstanceData instanceData;
        public ResourceType resourceType;

        public Resource( Vector2Int position, ResourceDataSO resourceDataSO) 
        {
            this.ResourceDataSO = resourceDataSO;
            this.instanceData = new ResourceInstanceData
            {
                id = new Guid(),
                amount = resourceDataSO.maxAmount,
                position = position,
            };
            this.resourceType = resourceDataSO.resourceType;
        }

        public string GetResourceName()
        {
            return ResourceDataSO.resourceName;
        }

        public int GetAmount()
        {
            return instanceData.amount;
        }

        public ResourceType GetResourceType()
        {
            return resourceType;
        }

        public void SetAmount(int amount)
        {
            instanceData.amount = amount;
        }

        protected virtual void Die()
        {
            // Logica di morte dell'entità (es. distruzione, animazione)
            Destroy(gameObject);
        }

        public override Guid GetId()
        {
            return instanceData.id;
        }

        public override int GetHealth()
        {
            return 1;
        }

        public override void SetHealth(int newHealth)
        {
            return;
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
        } // Le risorse sono sul terreno.

        public override void ExecuteCommand(ICommand command)
        {
            // Implementazione specifica per le risorse.
            Debug.Log("Resource ExecuteCommand: " + command.GetType().Name);
        }
    }
}