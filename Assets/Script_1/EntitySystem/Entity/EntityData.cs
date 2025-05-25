using System.Collections.Generic;

namespace Script.EntitySystem.Entity
{
    [System.Serializable]
    public class EntityData
    {
        public string type;
        public Dictionary<string, object> properties;

        public EntityData(string type, Dictionary<string, object> properties)
        {
            this.type = type;
            this.properties = properties;
        }
    }
}