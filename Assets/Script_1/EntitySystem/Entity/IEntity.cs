using System;
using UnityEngine;

namespace Script.EntitySystem.Entity
{
    public interface IEntity
    {
        Guid GetId();
        int GetHealth();
        void SetHealth(int newHealth);
        Vector2Int GetPosition();
        
        void SetPosition(Vector2Int newPosition);
        
        int GetZorder();
        void ExecuteCommand(ICommand command);
       }
}