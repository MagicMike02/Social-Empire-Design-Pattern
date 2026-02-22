using System.Collections.Generic;
using Script.GridSystem;
using Script.ResourceSystem.Enums;
using Script.ResourceSystem.ResourceGenerationStrategy;
using UnityEngine;
using VContainer;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Spawna risorse sulla griglia usando strategy pattern.
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        #region Dependencies (Injected by VContainer)
        
        private TileManager _tileManager;
        private ZoneManager _zoneManager;
        private ResourcePoolManager _poolManager;

        [Inject]
        public void Construct(TileManager tileManager, ZoneManager zoneManager, ResourcePoolManager poolManager)
        {
            _tileManager = tileManager;
            _zoneManager = zoneManager;
            _poolManager = poolManager;
        }
        
        #endregion

        #region Configuration
        
        [SerializeField] private List<ResourceDataSO> _resourceTypes;
        
        #endregion

        #region Private Fields & Events

        private IResourceGenerationStrategy _generationStrategy;

        /// <summary>
        /// Delegato e evento per notificare lo spawn formale di una risorsa.
        /// </summary>
        public delegate void ResourceSpawned(ResourceType type, Vector2Int pos, GameObject instance);
        public event ResourceSpawned OnResourceSpawned;
        
        #endregion

        #region Public APIs

        /// <summary>
        /// Imposta la strategia di generazione corrente.
        /// </summary>
        public void SetGenerationStrategy(IResourceGenerationStrategy strategy)
        {
            _generationStrategy = strategy;
        }

        /// <summary>
        /// Avvia il processo generale di generazione risorse iterando sui setup ScriptableObject.
        /// </summary>
        public void GenerateAllResources()
        {
            Debug.Log("Starting resource generation...");
            foreach (var resource in _resourceTypes)
            {
                GenerateResourceGroups(resource);
            }
        }

        #endregion

        #region Internal Generation Logic

        /// <summary>
        /// Esegue tentativi di spawn di gruppi di risorse basati sul config SO.
        /// </summary>
        private void GenerateResourceGroups(ResourceDataSO resource)
        {
            int attempts = 0;
            int groupsCreated = 0;
            int width = _tileManager.Width;
            int height = _tileManager.Height;

            while (groupsCreated < resource.groupCount && attempts < resource.groupCount * 20)
            {
                attempts++;
                Vector2Int origin = GetRandomTileOutsideCentralZone(width, height);
                int groupSize = resource.possibleGroupSizes[Random.Range(0, resource.possibleGroupSizes.Count)];

                if (resource.isDestroyedOnCollect)
                {
                    groupSize = resource.defaultGroupSize;
                    SetGenerationStrategy(new RegularGridWithSingleRandomGenerationStrategy());
                }
                else
                {
                    SetGenerationStrategy(new ClusterGenerationStrategy());
                }

                if (_generationStrategy == null)
                    return;

                List<Vector2Int> positions = _generationStrategy.GenerateResourcePositions(origin, groupSize);
                if (positions.TrueForAll(IsValidTile))
                {
                    foreach (var pos in positions)
                    {
                        SpawnResourceAt(pos, resource);
                    }

                    groupsCreated++;
                }
            }
        }

        /// <summary>
        /// Sceglie coordinate di griglia casuali non appartenenti alla safe-zone centrale.
        /// </summary>
        private Vector2Int GetRandomTileOutsideCentralZone(int width, int height)
        {
            while (true)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                if (!IsInsideCentralZone(x, y, width, height)) return new Vector2Int(x, y);
            }
        }

        /// <summary>
        /// Controlla validita' della zona safe centrale protetta da spawn.
        /// </summary>
        private bool IsInsideCentralZone(int x, int y, int width, int height)
        {
            int half = _zoneManager.ZoneSize / 2;
            return x >= width / 2 - half &&
                   x < width / 2 + half &&
                   y >= height / 2 - half &&
                   y < height / 2 + half;
        }

        /// <summary>
        /// Verifica che la tile esista, non sia ancora comprata dal player, e priva di ostacoli originari.
        /// </summary>
        private bool IsValidTile(Vector2Int pos)
        {
            var tile = _tileManager.GetGrid().GetValue(pos.x, pos.y);
            return tile != null
                   && !_zoneManager.occupiedTiles.ContainsKey(pos)
                   && tile.State != TileState.Unlocked;
        }

        /// <summary>
        /// Istanzia fisicamente (o riprende dal pool) il prefab di risorsa e invoca l'evento di spawn.
        /// </summary>
        private void SpawnResourceAt(Vector2Int gridPos, ResourceDataSO data)
        {
            Tile tile = _tileManager.GetGrid().GetValue(gridPos.x, gridPos.y);
            if (!tile) return;

            Vector3 worldPos = tile.transform.position;
            var position = worldPos + new Vector3(0, data.yOffset, 0);

            GameObject resource = _poolManager
                ? _poolManager.GetFromPool(data, position, Quaternion.identity)
                : Instantiate(data.GetRandomPrefab(), position, Quaternion.identity, transform);

            if (!resource) return;

            OnResourceSpawned?.Invoke(data.resourceType, gridPos, resource);
        }

        /// <summary>
        /// Fallback pubblico che rilancia `SpawnResourceAt`.
        /// </summary>
        public void SpawnResourceAtPosition(Vector2Int gridPos, ResourceDataSO data)
        {
            SpawnResourceAt(gridPos, data);
        }

        /// <summary>
        /// Ritorna la configurazione (SO) della risorsa mappata tramite l'enum ResourceType.
        /// </summary>
        public ResourceDataSO GetResourceDataSO(ResourceType type)
        {
            foreach (var data in _resourceTypes)
            {
                if (data.resourceType == type)
                    return data;
            }

            return null;
        }
        
        #endregion
    }
}