using System;

namespace Script.EntitySystem.Unit
{
    public struct UnitData
    {
        public Guid id;
        public string name;
        public int maxHealth;
        public int health;
        public int attackDamage;
        public int attackRange;
        public float attackSpeed;
        public float speed;
    }
}