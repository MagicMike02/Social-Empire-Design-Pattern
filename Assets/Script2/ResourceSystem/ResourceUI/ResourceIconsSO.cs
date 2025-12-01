using UnityEngine;
using System.Collections.Generic;
using Script2.ResourceSystem.Enums;

namespace Script2.ResourceSystem.ResourceUI
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

            Debug.LogWarning($"Icon not found for resource type: {type}");
            return null; // icona di default per le risorse mancanti
        }
    }
}