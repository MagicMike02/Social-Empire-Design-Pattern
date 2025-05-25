using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script.ScriptableObject
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Data/Unit", order = 1)]
    public class UnitDataSO : UnityEngine.ScriptableObject
    {
        public UnitType unitType;
        public string unitName;
        public int maxHealth;  //cambiato da health a maxHealth
        public int attackDamage;
        public int attackRange;
        public float attackSpeed;

        public float speed;
    }
}