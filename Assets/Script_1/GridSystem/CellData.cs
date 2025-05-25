using Script.EntitySystem.Entity;

namespace Script.GridSystem
{
    [System.Serializable]
    public class CellData
    {
        public string terrain;
        public string resource;
        public EntityData entity;

        public CellData(string terrain, string resource, EntityData entity)
        {
            this.terrain = terrain;
            this.resource = resource;
            this.entity = entity;
        }
        
        public CellData( string resource, EntityData entity)
        {
            this.resource = resource;
            this.entity = entity;
        }
    }
}