using System.Collections.Generic;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Resource;

namespace Script.GridSystem
{
    public class Cell
    {
        private Terrain terrain;
        private IEntity entity;
        private bool isWalkable;
        private List<Zone> zones;
        private bool isUnlocked;
        private ResourceType resource;
        private float resourceRegenerationTimer;
        private int resourceMaxAmount;

        public Cell(Terrain terrain)
        {
            this.terrain = terrain;
            this.isWalkable = terrain.IsPassable();
            this.zones = new List<Zone>();
            this.isUnlocked = true;
            this.resource = ResourceType.None;
            this.resourceRegenerationTimer = 0f;
            this.resourceMaxAmount = 0;
        }

        public Terrain GetTerrain() { return terrain; }
        public void SetTerrain(Terrain terrain) { this.terrain = terrain; }
        public IEntity GetEntity() { return entity; }
        public void SetEntity(IEntity entity) { this.entity = entity; }
        public bool IsOccupied() { return entity != null; }
        public bool IsWalkable() { return isWalkable; }
        public void SetWalkable(bool walkable) { this.isWalkable = walkable; }
        public bool IsUnlocked() { return isUnlocked; }
        public void SetUnlocked(bool unlocked) { this.isUnlocked = unlocked; }
        public void AddZone(Zone zone) { this.zones.Add(zone); }
        public void RemoveZone(Zone zone) { this.zones.Remove(zone); }
        public List<Zone> GetZones() { return zones; }
        public ResourceType GetResource() { return resource; }
        public void SetResource(ResourceType resource) { this.resource = resource; }
        public float GetResourceRegenerationTimer() { return resourceRegenerationTimer; }
        public void SetResourceRegenerationTimer(float timer) { this.resourceRegenerationTimer = timer; }
        public int GetResourceMaxAmount() { return resourceMaxAmount; }
        public void SetResourceMaxAmount(int amount) { this.resourceMaxAmount = amount; }
    }
}