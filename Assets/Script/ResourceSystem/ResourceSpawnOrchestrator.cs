using System.Collections.Generic;
using Script.GridSystem;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Coordina il ciclo di vita delle risorse attive e delle rigenerazioni.
    /// Mantiene lo stato fuori dal MonoBehaviour principale per alleggerire ResourceManager.
    /// </summary>
    public sealed class ResourceSpawnOrchestrator
    {
        private readonly TileManager _tileManager;
        private readonly ResourceSpawner _resourceSpawner;
        private readonly ResourcePoolManager _poolManager;
        private readonly GridManager _gridManager;
        private readonly Transform _ownerTransform;

        private readonly Dictionary<Vector2Int, GameObject> _activeResources = new();
        private readonly ResourceRegenerationController _regenerationController = new();
        private readonly List<ResourceRegenerationJob> _completedRegenerations = new();

        public ResourceSpawnOrchestrator(
            TileManager tileManager,
            ResourceSpawner resourceSpawner,
            ResourcePoolManager poolManager,
            GridManager gridManager,
            Transform ownerTransform)
        {
            _tileManager = tileManager;
            _resourceSpawner = resourceSpawner;
            _poolManager = poolManager;
            _gridManager = gridManager;
            _ownerTransform = ownerTransform;
        }

        public int ActiveCount => _activeResources.Count;

        public bool HasResourceAt(Vector2Int cell)
        {
            return _activeResources.ContainsKey(cell);
        }

        public void TrackResource(Vector2Int position, GameObject instance)
        {
            _activeResources[position] = instance;
        }

        public void ForgetResource(Vector2Int position)
        {
            _activeResources.Remove(position);
        }

        public void HandleResourceSpawned(ResourceType type, Vector2Int pos, GameObject instance, IResourceCollectionHandler collectionHandler)
        {
            TrackResource(pos, instance);
            _gridManager.OccupyCell(pos, instance);

            var ri = instance.GetComponent<ResourceInstance>();
            if (ri)
            {
                ri.Initialize(_resourceSpawner.GetResourceDataSO(type), pos, collectionHandler);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError($"[ResourceSpawnOrchestrator] ResourceInstance component non trovato su {instance.name}!");
#endif
            }
        }

        public void HandleResourceCollected(Vector2Int pos, ResourceDataSO data)
        {
            RemoveResource(pos);

            if (!data.isDestroyedOnCollect)
            {
                ScheduleRegeneration(pos, data);
            }
            else
            {
                _gridManager.FreeCell(pos);
            }
        }

        public void Tick(float deltaTime)
        {
            _completedRegenerations.Clear();
            _regenerationController.Tick(deltaTime, _completedRegenerations);
        }

        public bool TryDequeueCompletedRegeneration(out ResourceRegenerationJob regen)
        {
            if (_completedRegenerations.Count == 0)
            {
                regen = default;
                return false;
            }

            regen = _completedRegenerations[0];
            _completedRegenerations.RemoveAt(0);
            return true;
        }

        public void CompleteRegeneration(ResourceRegenerationJob regen)
        {
            if (regen.VisualObject)
            {
                Object.Destroy(regen.VisualObject);
            }

            _gridManager.FreeCell(regen.Position);
            ForgetResource(regen.Position);
            _resourceSpawner.SpawnResourceAtPosition(regen.Position, regen.Data);
        }

        public void Clear()
        {
            _regenerationController.Clear();
            _completedRegenerations.Clear();
        }

        public void RemoveAllResources()
        {
            foreach (var resource in _activeResources)
            {
                _gridManager.FreeCell(resource.Key);

                if (resource.Value)
                {
                    Object.Destroy(resource.Value);
                }
            }

            _activeResources.Clear();
            Clear();
        }

        private void RemoveResource(Vector2Int pos)
        {
            if (_activeResources.TryGetValue(pos, out var go) && go)
            {
                var ri = go.GetComponent<ResourceInstance>();
                var data = ri != null ? ri.Data : null;

                if (_poolManager && data)
                {
                    _poolManager.ReturnToPool(go, data);
                }
                else
                {
                    Object.Destroy(go);
                }
            }

            ForgetResource(pos);
        }

        private void ScheduleRegeneration(Vector2Int pos, ResourceDataSO data)
        {
            if (_regenerationController.HasPendingAt(pos))
            {
                return;
            }

            Tile tile = _tileManager.GetGrid().GetValue(pos.x, pos.y);
            GameObject regenVisual = null;

            if (tile && data._regenPrefab)
            {
                regenVisual = Object.Instantiate(
                    data._regenPrefab,
                    tile.transform.position + new Vector3(0, data.yOffset, 0),
                    Quaternion.identity,
                    _ownerTransform);

                TrackResource(pos, regenVisual);
                _gridManager.OccupyCell(pos, regenVisual);
            }

            _regenerationController.Add(pos, data, data.regenerationTime, regenVisual);
        }
    }
}
