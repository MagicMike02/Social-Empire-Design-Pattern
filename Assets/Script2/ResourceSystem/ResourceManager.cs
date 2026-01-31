﻿using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Script2.GridSystem;
using Script2.Economy;
using Script2.ResourceSystem.Enums;
using VContainer;

namespace Script2.ResourceSystem
{
    /// <summary>
    /// Gestisce risorse di gioco: spawning, collection, regeneration.
    /// REFACTORED: Usa Dependency Injection (VContainer) invece di Inspector-driven dependencies.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        #region Dependencies (Injected by VContainer)
        
        private TileManager _tileManager;
        private GameEconomyManager _economyManager;
        private ZoneManager _zoneManager;
        private ResourceSpawner _resourceSpawner;
        private ResourcePoolManager _poolManager;

        [Inject]
        public void Construct(
            TileManager tileManager,
            GameEconomyManager economyManager,
            ZoneManager zoneManager,
            ResourceSpawner resourceSpawner,
            ResourcePoolManager poolManager)
        {
            _tileManager = tileManager;
            _economyManager = economyManager;
            _zoneManager = zoneManager;
            _resourceSpawner = resourceSpawner;
            _poolManager = poolManager;
        }
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<Vector2Int, GameObject> _activeResources = new();
        private Dictionary<Vector2Int, Coroutine> _regenerationCoroutines = new();
        
        #endregion

        #region Events
        
        public event Action<ResourceType, int, Vector2Int> OnResourceCollected;
        public event Action<ResourceType, Vector2Int> OnResourceGenerated;
        public event Action<Vector2Int, float> OnRegenerationStarted;
        public event Action<Vector2Int, ResourceType> OnResourceRegenerated;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateDependencies();
        }
        
        private void Start()
        {
            InitializeResourceSystem();
        }
        
        #endregion

        #region Initialization
        
        private void ValidateDependencies()
        {
            if (_tileManager == null)
                Debug.LogError("[ResourceManager] TileManager non iniettato! VContainer dovrebbe averlo fornito.");
            
            if (_economyManager == null)
                Debug.LogError("[ResourceManager] GameEconomyManager non iniettato! VContainer dovrebbe averlo fornito.");
            
            if (_zoneManager == null)
                Debug.LogError("[ResourceManager] ZoneManager non iniettato! VContainer dovrebbe averlo fornito.");
            
            if (_resourceSpawner == null)
                Debug.LogError("[ResourceManager] ResourceSpawner non iniettato! VContainer dovrebbe averlo fornito.");
            
            if (_poolManager == null)
                Debug.LogError("[ResourceManager] ResourcePoolManager non iniettato! VContainer dovrebbe averlo fornito.");
        }

        private void InitializeResourceSystem()
        {
            if (_resourceSpawner == null)
            {
                Debug.LogError("[ResourceManager] Impossibile inizializzare: ResourceSpawner è NULL!");
                return;
            }
            
            _resourceSpawner.OnResourceSpawned += HandleResourceSpawned;
            Debug.Log("[ResourceManager] ✓ Generazione risorse in corso...");
            _resourceSpawner.GenerateAllResources();
        }
        
        #endregion

        #region Resource Management

        private void HandleResourceSpawned(ResourceType type, Vector2Int pos, GameObject instance)
        {
            _activeResources[pos] = instance;
            _zoneManager.occupiedTiles.Add(pos, instance);
            var ri = instance.GetComponent<ResourceInstance>();
            
            if (ri)
            {
                ri.Initialize(_resourceSpawner.GetResourceDataSO(type), pos, this);
            }
            else
            {
                Debug.LogError($"[ResourceManager] ResourceInstance component non trovato su {instance.name}!");
            }
            
            OnResourceGenerated?.Invoke(type, pos);
        }

        public void HandleResourceCollected(Vector2Int pos, ResourceDataSO data)
        {
            UpdateEconomy(data);
            RemoveResource(pos);

            OnResourceCollected?.Invoke(data.resourceType, data.collectedAmount, pos);

            if (!data.isDestroyedOnCollect)
                ScheduleRegeneration(pos, data);
        }

        private void RemoveResource(Vector2Int pos)
        {
            if (_activeResources.TryGetValue(pos, out var go) && go)
            {
                var data = GetResourceDataForInstance(go);
                if (_poolManager && data)
                    _poolManager.ReturnToPool(go, data);
                else
                    Destroy(go);
            }
            _zoneManager.occupiedTiles.Remove(pos);
            _activeResources.Remove(pos);
        }

        private void UpdateEconomy(ResourceDataSO data)
        {
            if (_economyManager != null)
            {
                _economyManager.AddResource(data.resourceType, data.collectedAmount);
            }
            else
            {
                Debug.LogError("[ResourceManager] GameEconomyManager non disponibile! Le risorse non verranno aggiunte all'economia.");
            }
        }
        
        #endregion

        #region Regeneration System

        private void ScheduleRegeneration(Vector2Int pos, ResourceDataSO data)
        {
            if (_regenerationCoroutines.TryGetValue(pos, out Coroutine existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
            }

            _regenerationCoroutines[pos] = StartCoroutine(RegenResourceAfterDelay(pos, data));
            OnRegenerationStarted?.Invoke(pos, data.regenerationTime);
        }

        private IEnumerator RegenResourceAfterDelay(Vector2Int pos, ResourceDataSO data)
        {
            // Istanzia prefab regen (visual only, no ResourceInstance needed)
            Tile tile = _tileManager.GetGrid().GetValue(pos.x, pos.y);
            if (!tile) yield break;

            Vector3 worldPos = tile.transform.position;
            GameObject regenPrefab = data._regenPrefab;
            GameObject regenVisual = null;
            
            if (regenPrefab)
            {
                regenVisual = Instantiate(regenPrefab, worldPos + new Vector3(0, data.yOffset, 0),
                    Quaternion.identity, transform);
                _activeResources[pos] = regenVisual;  // Track visual temporarily
            }

            yield return new WaitForSeconds(data.regenerationTime);

            // Cleanup regen visual
            if (regenVisual)
            {
                Destroy(regenVisual);
            }
            
            _zoneManager.occupiedTiles.Remove(pos);
            _activeResources.Remove(pos);

            // Spawn the actual resource (original prefab with ResourceInstance)
            _resourceSpawner.SpawnResourceAtPosition(pos, data);
            
            OnResourceRegenerated?.Invoke(pos, data.resourceType);
            _regenerationCoroutines.Remove(pos);
        }
        
        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Previene orphaned coroutines e memory leaks su scene transitions
            foreach (var coroutine in _regenerationCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }

            _regenerationCoroutines.Clear();
            
            // Disiscrivi dall'evento per prevenire stale references
            if (_resourceSpawner != null)
            {
                _resourceSpawner.OnResourceSpawned -= HandleResourceSpawned;
            }
            
            // Cleanup eventi per prevenire memory leaks
            OnResourceCollected = null;
            OnResourceGenerated = null;
            OnRegenerationStarted = null;
            OnResourceRegenerated = null;
        }
        
        #endregion

        #region Editor Utilities

        [ContextMenu("Remove All Resources")]
        private void RemoveAllResources()
        {
            foreach (var resource in _activeResources)
            {
                // Libero la cella occupata 
                _zoneManager.occupiedTiles.Remove(resource.Key);

                // Distruggo il GameObject
                Destroy(resource.Value);
            }

            _activeResources.Clear();
            Debug.Log("[ResourceManager] All resources have been removed.");
        }

        [ContextMenu("Regenerate All Resources")]
        private void RegenerateAllResources()
        {
            RemoveAllResources(); // Elimina tutte le risorse
            _resourceSpawner.GenerateAllResources(); // Rigenera tutte le risorse
            Debug.Log("[ResourceManager] All resources have been regenerated.");
        }

        #endregion

        #region Helper Methods

        private ResourceDataSO GetResourceDataForInstance(GameObject go)
        {
            var ri = go.GetComponent<ResourceInstance>();
            return ri != null ? _resourceSpawner.GetResourceDataSO(ri.Data.resourceType) : null;
        }
        
        #endregion

        #region Pathfinding Support
        
        /// <summary>
        /// Verifica se una cella contiene una risorsa (ostacolo per pathfinding).
        /// </summary>
        public bool HasResourceAt(Vector2Int cell)
        {
            return _activeResources.ContainsKey(cell);
        }
        
        #endregion
    }
}