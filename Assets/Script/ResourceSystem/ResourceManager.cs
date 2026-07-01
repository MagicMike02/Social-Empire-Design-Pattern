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
        private List<ActiveRegeneration> _activeRegenerations = new();
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (!ValidateDependencies())
            {
                return;
            }
        }

        private void OnEnable()
        {
            if (_resourceSpawner != null)
                _resourceSpawner.OnResourceSpawned += HandleResourceSpawned;
        }

        private void OnDisable()
        {
            if (_resourceSpawner != null)
                _resourceSpawner.OnResourceSpawned -= HandleResourceSpawned;
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
            ProcessRegenerations();
        }
        
        #endregion

        #region Initialization
        
        private bool ValidateDependencies()
        {
            bool isValid = true;

            if (_tileManager == null)
            {
                Debug.LogError("[ResourceManager] TileManager non iniettato! VContainer dovrebbe averlo fornito.");
                isValid = false;
            }
            
            if (_economyManager == null)
            {
                Debug.LogError("[ResourceManager] GameEconomyManager non iniettato! VContainer dovrebbe averlo fornito.");
                isValid = false;
            }
            
            if (_zoneManager == null)
            {
                Debug.LogError("[ResourceManager] ZoneManager non iniettato! VContainer dovrebbe averlo fornito.");
                isValid = false;
            }
            
            if (_resourceSpawner == null)
            {
                Debug.LogError("[ResourceManager] ResourceSpawner non iniettato! VContainer dovrebbe averlo fornito.");
                isValid = false;
            }
            
            if (_poolManager == null)
            {
                Debug.LogError("[ResourceManager] ResourcePoolManager non iniettato! VContainer dovrebbe averlo fornito.");
                isValid = false;
            }

            return isValid;
        }

        private void InitializeResourceSystem()
        {
            if (_resourceSpawner == null)
            {
                Debug.LogError("[ResourceManager] Impossibile inizializzare: ResourceSpawner è NULL!");
                return;
            }
            
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
        /// Aggiunge una risorsa alla coda di rigenerazione governata dal tick Update() invece che da coroutine.
        /// </summary>
        private void ScheduleRegeneration(Vector2Int pos, ResourceDataSO data)
        {
            // Previene ri-registrazioni duplicate sulla stessa cella (non dovrebbe succedere poichè la grid è bloccata)
            for (int i = 0; i < _activeRegenerations.Count; i++)
            {
                if (_activeRegenerations[i].Position == pos)
                {
                    return; // Sta già rigenerando
                }
            }

            // Spawna eventuale modello statico di rigenerazione (es. tronco tagliato)
            Tile tile = _tileManager.GetGrid().GetValue(pos.x, pos.y);
            GameObject regenVisual = null;

            if (tile && data._regenPrefab)
            {
                regenVisual = Instantiate(data._regenPrefab, tile.transform.position + new Vector3(0, data.yOffset, 0), Quaternion.identity, transform);
                _activeResources[pos] = regenVisual;
                _gridManager.OccupyCell(pos, regenVisual);
            }

            _activeRegenerations.Add(new ActiveRegeneration(pos, data, data.regenerationTime, regenVisual));
            
            // Pubblica evento GlobalEventBus per UI/Audio
            GlobalEventBus.Publish(new ResourceRegenerationStartedEvent(pos, data.regenerationTime));
        }

        /// <summary>
        /// Tick loop eseguito ad ogni frame. Scalando il tempo di ogni rigenerazione attiva.
        /// </summary>
        private void ProcessRegenerations()
        {
            if (_activeRegenerations.Count == 0) return;

            float dt = Time.deltaTime;

            // Iterazione inversa sicura per rimuovere elementi in-place senza spezzare l'indice
            for (int i = _activeRegenerations.Count - 1; i >= 0; i--)
            {
                var regen = _activeRegenerations[i];
                regen.TimeLeft -= dt;

                if (regen.TimeLeft <= 0f)
                {
                    CompleteRegeneration(regen);
                    _activeRegenerations.RemoveAt(i);
                }
                else
                {
                    // Aggiorna lo struct all'interno della lista 
                    // (Poiché struct è pass-by-value, l'elemento va riassegnato)
                    _activeRegenerations[i] = regen;
                }
            }
        }

        private void CompleteRegeneration(ActiveRegeneration regen)
        {
            // Pulisci l'ostacolo/sprite temporaneo
            if (regen.VisualObject)
            {
                Destroy(regen.VisualObject);
            }
            
            _gridManager.FreeCell(regen.Position);
            _activeResources.Remove(regen.Position);

            // Spawna definitivamente il nuovo albero instanziando ResourceInstance class!
            _resourceSpawner.SpawnResourceAtPosition(regen.Position, regen.Data);
            
            GlobalEventBus.Publish(new ResourceRegeneratedEvent(regen.Position, regen.Data.resourceType));
        }
        
        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup per liberare sottoscrizioni ad eventi e deregistrare l'esecuzione asincrona per evitare side effects.
        /// </summary>
        private void OnDestroy()
        {
            _activeRegenerations.Clear();
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

        #region Inner Classes
        
        /// <summary>
        /// Struct compatta basata su dati che tiene traccia del progresso di rigenerazione di una risorsa.
        /// Essendo uno struct limita l'overhead del Garbage Collector per allocazioni sul mucchio ad ogni albero spezzato.
        /// </summary>
        private struct ActiveRegeneration
        {
            public Vector2Int Position;
            public ResourceDataSO Data;
            public float TimeLeft;
            public GameObject VisualObject;

            public ActiveRegeneration(Vector2Int position, ResourceDataSO data, float timeLeft, GameObject visualObject)
            {
                Position = position;
                Data = data;
                TimeLeft = timeLeft;
                VisualObject = visualObject;
            }
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
