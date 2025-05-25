using System;
using UnityEngine;

namespace Script.EntitySystem.Entity
{
    public abstract class Entity : MonoBehaviour, IEntity
    {
        public abstract Guid GetId();
        public abstract int GetHealth();
        public abstract void SetHealth(int newHealth);

        public abstract Vector2Int GetPosition();
        public abstract void SetPosition(Vector2Int newPosition);        
        
        public abstract int GetZorder();
        public abstract void ExecuteCommand(ICommand command);
    }
}