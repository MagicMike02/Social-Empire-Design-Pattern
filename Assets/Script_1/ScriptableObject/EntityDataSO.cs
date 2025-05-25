using UnityEngine;
namespace Script.ScriptableObject
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "Data/Entity", order = 1)]
    public class EntityDataSO : UnityEngine.ScriptableObject
    {
        public string entityName;
        public int maxHealth;  //cambiato da health a maxHealth
        public int attackDamage;
        public float speed;
    }
}