using UnityEngine;
using VContainer;
using Script.Common;

namespace Script.BuildingSystem
{
    public sealed class BuildingFactory : MonoBehaviour
    {
        private PrefabPoolManager _poolManager;

        [Inject]
        public void Construct(PrefabPoolManager poolManager)
        {
            _poolManager = poolManager;
        }
        public Building CreateBuilding(BuildingConfigSO config, Vector3 worldPos, Transform parent = null)
        {
            if (config == null || config.Prefab == null)
            {
                Debug.LogError($"[BuildingFactory] Impossibile creare edificio: config o prefab mancante!");
                return null;
            }

            if (_poolManager == null)
            {
                Debug.LogError("[BuildingFactory] PoolManager non inizializzato!");
                return null;
            }

            var buildingGO = _poolManager.Get(config.Prefab, worldPos, Quaternion.identity, parent);
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
