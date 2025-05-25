using Script.EntitySystem.Resource;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public class ResourceCollectedEvent : GridEvent
    {
        private ResourceType resourceType;
        private int amount;

        public ResourceCollectedEvent(Vector2Int position, ResourceType resourceType, int amount) : base(position)
        {
            this.resourceType = resourceType;
            this.amount = amount;
        }

        public ResourceType GetResourceType() { return resourceType; }
        public int GetAmount() { return amount; }
    }
}