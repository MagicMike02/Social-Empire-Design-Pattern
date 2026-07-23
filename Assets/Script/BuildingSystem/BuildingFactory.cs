using UnityEngine;
using VContainer;
using Script.Common;
using Script.Core.Entities;

namespace Script.BuildingSystem
{
    public sealed class BuildingFactory : MonoBehaviour
    {
        private PrefabPoolManager _poolManager;

        [Inject]
        public void Construct(PrefabPoolManager poolManager)
        {
            try
            {
                _poolManager = poolManager;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[BuildingFactory] Errore durante Construct: {ex.Message}");
#endif
            }
        }
        public Building CreateBuilding(BuildingConfigSO config, Vector3 worldPos, Transform parent = null)
        {
            if (config == null || config.Prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[BuildingFactory] Impossibile creare edificio: config o prefab mancante!");
#endif
                return null;
            }

            if (_poolManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BuildingFactory] PoolManager non inizializzato!");
#endif
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

            // Sincronizza i dati display dell'EntityInfoProvider con il SO (se presente).
            // I componenti entity (Selectable/Highlightable/EntityInfoProvider) sono già
            // cablati sul prefab Building_Base e derivati; qui si popolano i campi
            // migrati dal SO per evitare duplicazione manuale sull'Inspector.
            SyncEntityInfo(buildingGO, config);

            var spriteRenderer = buildingGO.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                IsometricSortingUtility.ApplySorting(spriteRenderer, config.SortingLayer, worldPos.y, config.BaseSortingOrder);
            }

            return building;
        }

        /// <summary>
        /// Popola <see cref="EntityInfoProvider"/> dai campi del <see cref="BuildingConfigSO"/>.
        /// Se il prefab non ha EntityInfoProvider (es. prefab legacy), il sync è no-op.
        /// </summary>
        private static void SyncEntityInfo(GameObject buildingGO, BuildingConfigSO config)
        {
            var infoProvider = buildingGO.GetComponent<EntityInfoProvider>();
            if (infoProvider == null) return;

            var iconPath = config.Icon != null ? config.Icon.name : string.Empty;
            infoProvider.Init(config.EntityName, config.Description, iconPath);
        }
    }
}
