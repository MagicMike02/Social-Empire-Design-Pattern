using System.Collections.Generic;
using UnityEngine;
using Script2.GridSystem;
using Script2.ResourceSystem.Enums;
using Script2.ResourceSystem.ResourceGenerationStrategy;

namespace Script2.ResourceSystem
{
    public class ResourceSpawner : MonoBehaviour
    {
        [SerializeField] private TileManager _tileManager;
        [SerializeField] private ZoneManager _zoneManager;
        [SerializeField] private ResourcePoolManager _poolManager;
        
        [SerializeField] private List<ResourceDataSO> _resourceTypes;
        
        private IResourceGenerationStrategy _generationStrategy;

        public delegate void ResourceSpawned(ResourceType type, Vector2Int pos, GameObject instance);
        public event ResourceSpawned OnResourceSpawned;

        public void SetGenerationStrategy(IResourceGenerationStrategy strategy)
        {
            _generationStrategy = strategy;
        }

        public void GenerateAllResources()
        {
            Debug.Log("Starting resource generation...");
            foreach (var resource in _resourceTypes)
            {
                GenerateResourceGroups(resource);
            }
        }

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

        private Vector2Int GetRandomTileOutsideCentralZone(int width, int height)
        {
            while (true)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                if (!IsInsideCentralZone(x, y, width, height)) return new Vector2Int(x, y);
            }
        }

        private bool IsInsideCentralZone(int x, int y, int width, int height)
        {
            int half = _zoneManager.ZoneSize / 2;
            return x >= width / 2 - half &&
                   x < width / 2 + half &&
                   y >= height / 2 - half &&
                   y < height / 2 + half;
        }

        private bool IsValidTile(Vector2Int pos)
        {
            var tile = _tileManager.GetGrid().GetValue(pos.x, pos.y);
            return tile != null
                   && !_zoneManager.occupiedTiles.ContainsKey(pos)
                   && tile.State != TileState.Unlocked;
        }

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

        public void SpawnResourceAtPosition(Vector2Int gridPos, ResourceDataSO data)
        {
            SpawnResourceAt(gridPos, data);
        }

        public ResourceDataSO GetResourceDataSO(ResourceType type)
        {
            foreach (var data in _resourceTypes)
            {
                if (data.resourceType == type)
                    return data;
            }

            return null;
        }
    }
}