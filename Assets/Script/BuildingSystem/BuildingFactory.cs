using UnityEngine;

namespace Script.BuildingSystem
{
    public sealed class BuildingFactory : MonoBehaviour
    {
        public Building CreateBuilding(BuildingConfigSO config, Vector3 worldPos, Transform parent = null)
        {
            if (config == null)
            {
                Debug.LogError("[BuildingFactory] Impossibile creare edificio: config è null!");
                return null;
            }

            if (config.Prefab == null)
            {
                Debug.LogError($"[BuildingFactory] Impossibile creare edificio '{config.name}': Prefab mancante!");
                return null;
            }

            var buildingGO = Instantiate(config.Prefab, worldPos, Quaternion.identity, parent);
            buildingGO.name = $"{config.name}_Instance";

            var building = buildingGO.GetComponent<Building>();
            if (building == null)
            {
                building = buildingGO.AddComponent<Building>();
            }

            building.Init(config);

            var spriteRenderer = buildingGO.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                IsometricSortingUtility.ApplySorting(spriteRenderer, config.SortingLayer, worldPos.y, config.BaseSortingOrder);
            }

            return building;
        }
    }
}
