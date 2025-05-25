using Script.EntitySystem.Resource;
using UnityEngine;

namespace Script.ScriptableObject
{
    [CreateAssetMenu(fileName = "ResourceData", menuName = "Data/Resource", order = 1)]
    public class ResourceDataSO : UnityEngine.ScriptableObject
    {
        public ResourceType resourceType;
        
        public string resourceName;
        public int maxAmount;
    }
}