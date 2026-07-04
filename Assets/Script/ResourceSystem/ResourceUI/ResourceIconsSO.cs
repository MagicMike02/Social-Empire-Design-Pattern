using UnityEngine;
using System.Collections.Generic;
using Script.ResourceSystem.Enums;

namespace Script.ResourceSystem.ResourceUI
{
    [CreateAssetMenu(fileName = "ResourceIcons", menuName = "ScriptableObjects/ResourceIcons")]
    public class ResourceIconsSO : ScriptableObject
    {
        [System.Serializable]
        public class ResourceIconEntry
        {
            public ResourceType type;
            public Sprite icon;
        }

        public List<ResourceIconEntry> icons;

        public Sprite GetIcon(ResourceType type)
        {
            foreach (var entry in icons)
            {
                if (entry.type == type)
                {
                    return entry.icon;
                }
            }

#if UNITY_EDITOR
            Debug.LogWarning($"Icon not found for resource type: {type}");
#endif
            return null; // icona di default per le risorse mancanti
        }
    }
}