using System;
using Script.ScriptableObject;
using UnityEngine;

namespace Script.EntitySystem.Unit
{
    public class Unit : Entity.Entity
    {
        public UnitDataSO UnitDataSO { get; private set; }
        public UnitInstanceData instanceData;
        public UnitType unitType;

        public Unit(Vector2Int position, UnitDataSO unitDataSO)
        {
            this.UnitDataSO = unitDataSO;
            this.instanceData = new UnitInstanceData
            {
                id = new Guid(),
                health = unitDataSO.maxHealth,
                position = position,
            };
            this.unitType = unitDataSO.unitType;
        }

        
        public string GetUnitName()
        {
            return UnitDataSO.unitName;
        }

        public override int GetHealth()
        {
            return instanceData.health;
        }

        public override void SetHealth(int newHealth)
        {
            instanceData.health = newHealth;
        }

        public int GetAttackDamage()
        {
            return UnitDataSO.attackDamage;
        }

        public float GetSpeed()
        {
            return UnitDataSO.speed;
        }

        public int GetMaxHealth()
        {
            return UnitDataSO.maxHealth;
        }

        public int GetAttackRange()
        {
            return UnitDataSO.attackRange;
        }

        public float GetAttackSpeed()
        {
            return UnitDataSO.attackSpeed;
        }

        public override Vector2Int GetPosition()
        {
            return instanceData.position;
        } 
        public override void SetPosition(Vector2Int newPosition)
        {
            instanceData.position = newPosition;
        }
        
        public void ApplyDamage(int damage)
        {
            instanceData.health -= damage;
            if (instanceData.health <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            // Logica di morte dell'entità (es. Distruzione, animazione)
            Destroy(gameObject);
        }

        public override Guid GetId()
        {
            return instanceData.id;
        }

        public override int GetZorder()
        {
            return 1;
        }

        public override void ExecuteCommand(ICommand command)
        {
            // Implementazione specifica per le unità.
            Debug.Log($"Unit {GetUnitName()} ExecuteCommand: " + command.GetType().Name);
        }
    }
}