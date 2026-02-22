using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Script.Core.Events;
using Script.EconomySystem;
using Script.GridSystem;
using Script.ResourceSystem.Enums;
using VContainer;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Gestisce risorse di gioco: spawning, collection, regeneration.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        #region Dependencies (Injected by VContainer)
        
        private TileManager _tileManager;
        private GameEconomyManager _economyManager;
        private ZoneManager _zoneManager;
        private ResourceSpawner _resourceSpawner;
        private ResourcePoolManager _poolManager;
        private GridManager _gridManager;

        [Inject]
        public void Construct(
            TileManager tileManager,
            GameEconomyManager economyManager,
            ZoneManager zoneManager,
            ResourceSpawner resourceSpawner,
            ResourcePoolManager poolManager,
            GridManager gridManager)
        {
            _tileManager = tileManager;
            _economyManager = economyManager;
            _zoneManager = zoneManager;
            _resourceSpawner = resourceSpawner;
            _poolManager = poolManager;
            _gridManager = gridManager;
        }
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<Vector2Int, GameObject> _activeResources = new();
        private Dictionary<Vector2Int, Coroutine> _regenerationCoroutines = new();
        
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

        /// <summary>
        /// Sottoscrive all'evento di spawn per istanziare, posizionare sulla griglia e inizializzare la logica ResourceInstance.
        /// </summary>
        private void HandleResourceSpawned(ResourceType type, Vector2Int pos, GameObject instance)
        {
            _activeResources[pos] = instance;
            _gridManager.OccupyCell(pos, instance);
            var ri = instance.GetComponent<ResourceInstance>();
            
            if (ri)
            {
                ri.Initialize(_resourceSpawner.GetResourceDataSO(type), pos, this);
            }
            else
            {
                Debug.LogError($"[ResourceManager] ResourceInstance component non trovato su {instance.name}!");
            }
            
            // Pubblica evento GlobalEventBus
            GlobalEventBus.Publish(new ResourceGeneratedEvent(type, pos));
        }

        /// <summary>
        /// Innescato dalla raccolta manuale. Aggiorna l'economia, invalida la risorsa corrente e ne gestisce l'eventuale rigenerazione.
        /// </summary>
        public void HandleResourceCollected(Vector2Int pos, ResourceDataSO data)
        {
            UpdateEconomy(data);
            RemoveResource(pos);

            // Pubblica evento GlobalEventBus
            GlobalEventBus.Publish(new ResourceCollectedEvent(
                data.resourceType, 
                data.collectedAmount, 
                pos
            ));

            if (!data.isDestroyedOnCollect)
            {
                // Risorsa rigenera → mantieni occupazione durante rigenerazione
                ScheduleRegeneration(pos, data);
            }
            else
            {
                // Risorsa distrutta permanentemente → libera cella
                _gridManager.FreeCell(pos);
            }
        }

        /// <summary>
        /// Gestisce la logica di distruzione o rientro in pool dell'istanza fisica.
        /// </summary>
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
            
            _activeResources.Remove(pos);
        }

        /// <summary>
        /// Accumula l'ammontare raccolto nel GameEconomyManager dipendente.
        /// </summary>
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

        /// <summary>
        /// Pianifica una coroutine per la rigenerazione di una risorsa dopo essere stata raccolta, bloccandone lo spazio griglia temporaneamente.
        /// </summary>
        private void ScheduleRegeneration(Vector2Int pos, ResourceDataSO data)
        {
            if (_regenerationCoroutines.TryGetValue(pos, out Coroutine existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
            }

            _regenerationCoroutines[pos] = StartCoroutine(RegenResourceAfterDelay(pos, data));
            
            // Pubblica evento GlobalEventBus
            GlobalEventBus.Publish(new ResourceRegenerationStartedEvent(pos, data.regenerationTime));
        }

        /// <summary>
        /// Coroutine che sostituisce l'entita' raccolta in un object passivo di cooldown, per ripristinarla dopo il timer.
        /// </summary>
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
                
                // ✅ Mantieni cella occupata durante rigenerazione (previene placement edifici)
                _gridManager.OccupyCell(pos, regenVisual);
            }

            yield return new WaitForSeconds(data.regenerationTime);

            // Cleanup regen visual
            if (regenVisual)
            {
                Destroy(regenVisual);
            }
            
            _gridManager.FreeCell(pos);
            _activeResources.Remove(pos);

            // Spawn the actual resource (original prefab with ResourceInstance)
            _resourceSpawner.SpawnResourceAtPosition(pos, data);
            
            // Pubblica evento GlobalEventBus
            GlobalEventBus.Publish(new ResourceRegeneratedEvent(pos, data.resourceType));
            
            _regenerationCoroutines.Remove(pos);
        }
        
        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup per liberare sottoscrizioni ad eventi e deregistrare l'esecuzione asincrona per evitare side effects.
        /// </summary>
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
            
        }
        
        #endregion

        #region Editor Utilities

        /// <summary>
        /// [Debug] Distrugge tutte le istanze fisiche e rimuove i riferimenti nella matrice risorse attive.
        /// </summary>
        [ContextMenu("Remove All Resources")]
        private void RemoveAllResources()
        {
            foreach (var resource in _activeResources)
            {
                // Libera la cella occupata 
                _gridManager.FreeCell(resource.Key);

                // Distruggo il GameObject
                Destroy(resource.Value);
            }

            _activeResources.Clear();
            Debug.Log("[ResourceManager] All resources have been removed.");
        }

        /// <summary>
        /// [Debug] Forza la rigenerazione istantanea per bypassare il timer di cooldown.
        /// </summary>
        [ContextMenu("Regenerate All Resources")]
        private void RegenerateAllResources()
        {
            RemoveAllResources(); // Elimina tutte le risorse
            _resourceSpawner.GenerateAllResources(); // Rigenera tutte le risorse
            Debug.Log("[ResourceManager] All resources have been regenerated.");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Mappatura utility da GameObject (di una risorsa viva su mappa) a ResourceDataSO tramite Spawner.
        /// </summary>
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