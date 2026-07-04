using Script.Core.Events;
using Script.EconomySystem;
using Script.GridSystem;
using Script.ResourceSystem.Enums;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Gestisce le risorse di gioco: spawning, collection, regeneration.
    /// </summary>
    public class ResourceManager : MonoBehaviour, IResourceCollectionHandler
    {
        #region Dependencies (Injected by VContainer)

        private TileManager _tileManager;
        private GameEconomyManager _economyManager;
        private ZoneManager _zoneManager;
        private ResourceSpawner _resourceSpawner;
        private ResourcePoolManager _poolManager;
        private GridManager _gridManager;
        private ResourceSpawnOrchestrator _spawnOrchestrator;

        [Inject]
        public void Construct(
            TileManager tileManager,
            GameEconomyManager economyManager,
            ZoneManager zoneManager,
            ResourceSpawner resourceSpawner,
            ResourcePoolManager poolManager,
            GridManager gridManager)
        {
            try
            {
                _tileManager = tileManager;
                _economyManager = economyManager;
                _zoneManager = zoneManager;
                _resourceSpawner = resourceSpawner;
                _poolManager = poolManager;
                _gridManager = gridManager;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[ResourceManager] Errore durante Construct: {ex.Message}");
#endif
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!ValidateDependencies())
            {
                return;
            }

            _spawnOrchestrator = new ResourceSpawnOrchestrator(
                _tileManager,
                _resourceSpawner,
                _poolManager,
                _gridManager,
                transform);
        }

        private void OnEnable()
        {
            if (_resourceSpawner != null)
            {
                _resourceSpawner.OnResourceSpawned += HandleResourceSpawned;
            }
        }

        private void OnDisable()
        {
            if (_resourceSpawner != null)
            {
                _resourceSpawner.OnResourceSpawned -= HandleResourceSpawned;
            }
        }

        private void Start()
        {
            if (!ValidateDependencies())
            {
                return;
            }

            InitializeResourceSystem();
        }

        private void Update()
        {
            if (_spawnOrchestrator == null)
            {
                return;
            }

            _spawnOrchestrator.Tick(Time.deltaTime);

            while (_spawnOrchestrator.TryDequeueCompletedRegeneration(out var regen))
            {
                _spawnOrchestrator.CompleteRegeneration(regen);
                GlobalEventBus.Publish(new ResourceRegeneratedEvent(regen.Position, regen.Data.resourceType));
            }
        }

        private void OnDestroy()
        {
            _spawnOrchestrator?.RemoveAllResources();
        }

        #endregion

        #region Initialization

        private bool ValidateDependencies()
        {
            if (_tileManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] TileManager non iniettato! VContainer dovrebbe averlo fornito.");
#endif
                return false;
            }

            if (_economyManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] GameEconomyManager non iniettato! VContainer dovrebbe averlo fornito.");
#endif
                return false;
            }

            if (_zoneManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] ZoneManager non iniettato! VContainer dovrebbe averlo fornito.");
#endif
                return false;
            }

            if (_resourceSpawner == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] ResourceSpawner non iniettato! VContainer dovrebbe averlo fornito.");
#endif
                return false;
            }

            if (_poolManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] ResourcePoolManager non iniettato! VContainer dovrebbe averlo fornito.");
#endif
                return false;
            }

            return true;
        }

        private void InitializeResourceSystem()
        {
            if (_resourceSpawner == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] Impossibile inizializzare: ResourceSpawner è NULL!");
#endif
                return;
            }

#if UNITY_EDITOR
            Debug.Log("[ResourceManager] ✓ Generazione risorse in corso...");
#endif
            _resourceSpawner.GenerateAllResources();
        }

        #endregion

        #region Resource Management

        private void HandleResourceSpawned(ResourceType type, Vector2Int pos, GameObject instance)
        {
            if (_spawnOrchestrator == null)
            {
                return;
            }

            _spawnOrchestrator.HandleResourceSpawned(type, pos, instance, this);
            GlobalEventBus.Publish(new ResourceGeneratedEvent(type, pos));
        }

        public void HandleResourceCollected(Vector2Int pos, ResourceDataSO data)
        {
            UpdateEconomy(data);

            if (_spawnOrchestrator != null)
            {
                _spawnOrchestrator.HandleResourceCollected(pos, data);
            }

            GlobalEventBus.Publish(new ResourceCollectedEvent(
                data.resourceType,
                data.collectedAmount,
                pos
            ));

            if (!data.isDestroyedOnCollect)
            {
                GlobalEventBus.Publish(new ResourceRegenerationStartedEvent(pos, data.regenerationTime));
            }
        }

        private void UpdateEconomy(ResourceDataSO data)
        {
            if (_economyManager != null)
            {
                _economyManager.AddResource(data.resourceType, data.collectedAmount);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] GameEconomyManager non disponibile! Le risorse non verranno aggiunte all'economia.");
#endif
            }
        }

        #endregion

        #region Editor Utilities

        [ContextMenu("Remove All Resources")]
        private void RemoveAllResources()
        {
            _spawnOrchestrator?.RemoveAllResources();
#if UNITY_EDITOR
            Debug.Log("[ResourceManager] All resources have been removed.");
#endif
        }

        [ContextMenu("Regenerate All Resources")]
        private void RegenerateAllResources()
        {
            RemoveAllResources();
            _resourceSpawner.GenerateAllResources();
#if UNITY_EDITOR
            Debug.Log("[ResourceManager] All resources have been regenerated.");
#endif
        }

        #endregion

        #region Pathfinding Support

        public bool HasResourceAt(Vector2Int cell)
        {
            return _spawnOrchestrator != null && _spawnOrchestrator.HasResourceAt(cell);
        }

        #endregion
    }
}
