using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script.ScriptableObject
{
    [CreateAssetMenu(fileName = "MonsterData", menuName = "Data/Monster", order = 1)]
    public class MonsterDataSO : UnityEngine.ScriptableObject
    {
        public UnitType monsterType;
        public string monsterName;
        public int maxHealth;
        public int attackDamage;
        public float speed;
    }
}