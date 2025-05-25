using Script.EntitySystem.Building;
using UnityEngine;

namespace Script.ScriptableObject
{
    [CreateAssetMenu(fileName = "BuildingData", menuName = "Data/Building", order = 1)]
    public class BuildingDataSO : UnityEngine.ScriptableObject
    {
        public BuildingType buildingType;  
        
        public string buildingName;
        public int maxHealth;
        public int width;
        public int height;
    }
}