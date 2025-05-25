using Script.EntitySystem.Building;
using UnityEngine;
using ResourceType = Script.EntitySystem.Resource.ResourceType;

namespace Script.EntitySystem.Entity
{
    public interface IEntityFactory
    {
        IEntity CreateEntity(string entityType, Vector2Int position);
    }
}