using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Script2.GridSystem;
using Script2.Economy;
using Script2.ResourceSystem.Enums;
using Script2.ResourceSystem.ResourceGenerationStrategy;
using Random = UnityEngine.Random;

namespace Script2.ResourceSystem
{
    public class ResourceManager : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private List<ResourceDataSO> _resourceTypes;
        [SerializeField] private GameEconomyManager _economyManager;
        
        private Dictionary<Vector2Int, GameObject> _activeResources = new();
        private Dictionary<Vector2Int, Coroutine> _regenerationCoroutines = new();

        private IResourceGenerationStrategy _generationStrategy;

        public event System.Action<ResourceType, int, Vector2Int> OnResourceCollected;
        public event System.Action<ResourceType, Vector2Int> OnResourceGenerated;
        public event System.Action<Vector2Int, float> OnRegenerationStarted;
        public event System.Action<Vector2Int, ResourceType> OnResourceRegenerated;

        void Start()
        {
            Debug.Log("Resource Manager -> Generating Resources");
            GenerateAllResources();
        }

        public void SetGenerationStrategy(IResourceGenerationStrategy strategy)
        {
            _generationStrategy = strategy;
        }

        public void GenerateAllResources()
        {
            foreach (var resource in _resourceTypes)
            {
                Debug.Log($"Resource Manager -> Generating {resource.name}");
                GenerateResourceGroups(resource);
            }
        }

        void GenerateResourceGroups(ResourceDataSO resource)
        {
            int attempts = 0;
            int groupsCreated = 0;

            while (groupsCreated < resource.groupCount && attempts < resource.groupCount * 20)
            {
                attempts++;

                Vector2Int origin = GetRandomTileOutsideCentralZone();
                int groupSize = resource.possibleGroupSizes[Random.Range(0, resource.possibleGroupSizes.Count)];

                //Imposto strategy di creazione dei gruppi
                if (resource.isDestroyedOnCollect)
                {
                    groupSize = resource.defaultGroupSize;
                    SetGenerationStrategy(
                        new RegularGridWithSingleRandomGenerationStrategy()); //SetGenerationStrategy(new RegularGridGenerationStrategy());
                }
                else
                {
                    SetGenerationStrategy(new ClusterGenerationStrategy());
                }

                if (_generationStrategy == null)
                {
                    Debug.LogError($"Generation strategy not set for {resource.name}");
                    return;
                }

                List<Vector2Int> positions = _generationStrategy.GenerateResourcePositions(origin, groupSize);

                // Verifica che i tile siano validi e vuoti
                if (positions.All(IsValidTile))
                {
                    foreach (var pos in positions)
                    {
                        PlaceResourceAt(pos, resource);
                    }

                    groupsCreated++;
                }
            }
        }

        Vector2Int GetRandomTileOutsideCentralZone()
        {
            int width = _gridManager.width;
            int height = _gridManager.height;

            while (true)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                if (!IsInsideCentralZone(x, y)) return new Vector2Int(x, y);
            }
        }

        bool IsInsideCentralZone(int x, int y)
        {
            int half = _gridManager.GetZoneSize() / 2;
            return x >= _gridManager.width / 2 - half &&
                   x < _gridManager.width / 2 + half &&
                   y >= _gridManager.height / 2 - half &&
                   y < _gridManager.height / 2 + half;
        }

        bool IsValidTile(Vector2Int pos)
        {
            var tile = _gridManager.GetTile(pos.x, pos.y);
            return tile != null
                   && !_activeResources.ContainsKey(pos)
                   && !_gridManager.occupiedTiles.ContainsKey(pos)
                   && tile.State != TileState.Unlocked;
        }

        void PlaceResourceAt(Vector2Int gridPos, ResourceDataSO data)
        {
            Tile tile = _gridManager.GetTile(gridPos.x, gridPos.y);
            if (!tile) return;

            Vector3 worldPos = tile.transform.position;
            GameObject prefab = data.GetRandomPrefab();
            if (!prefab) return;

            GameObject resource = Instantiate(prefab, worldPos + new Vector3(0, data.yOffset, 0), Quaternion.identity,
                transform);
            //Debug.Log($"Resource {data.name} created at {tile.name}");

            _activeResources[gridPos] = resource;
            _gridManager.occupiedTiles.Add(gridPos, resource);

            var ri = resource.GetComponent<ResourceInstance>();
            if (!ri) return;

            ri.Initialize(data, gridPos, this);

            OnResourceGenerated?.Invoke(data.resourceType, gridPos);
        }

        /* public void OnResourceCollected(Vector2Int pos, ResourceDataSO data)
        {
            //Debug.Log($"ResourceManager - {data.name} collected at {pos}.");

            UpdateEconomy(data);

            //Elimino GameObject Raccolto
            RemoveResource(pos);

            //Se il Gameobject prevede una rigenerazione Creami il prefab di Regen e fai partire il timer per la rigenerazione
            if (!data.isDestroyedOnCollect)
            {
                ScheduleRegeneration(pos, data);
            }
        }*/

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
            if (_activeResources.TryGetValue(pos, out GameObject go) && go != null)
                Destroy(go);
            
            _gridManager.occupiedTiles.Remove(pos);
            _activeResources.Remove(pos);

            Destroy(go);
        }

        private static void UpdateEconomy(ResourceDataSO data)
        {
            if (GameEconomyManager.Instance)
            {
                GameEconomyManager.Instance.AddResource(data.resourceType, data.collectedAmount);
            }
            else
            {
                Debug.LogError("GameEconomyManager instance not found! Resources will not be added to economy.");
            }
        }

        private void ScheduleRegeneration(Vector2Int pos, ResourceDataSO data)
        {
            Tile tile = _gridManager.GetTile(pos.x, pos.y);
            if (tile == null) return;

            Vector3 worldPos = tile.transform.position;
            GameObject regenPrefab = data._regenPrefab;
            if (regenPrefab == null) return;

            //Instanzio il prefab di regen
            GameObject regenResource = Instantiate(regenPrefab, worldPos + new Vector3(0, data.yOffset, 0),
                Quaternion.identity, transform);
            Debug.Log(
                $"RegenResource {data.name} created at {tile.name} -> Regenerating in {data.regenerationTime} seconds at {pos}");

            _activeResources[pos] = regenResource;
            _gridManager.occupiedTiles.Add(pos, regenResource);

            if (_regenerationCoroutines.TryGetValue(pos, out Coroutine existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
                Debug.LogWarning($"Stopped existing regeneration at {pos}");
            }

            _regenerationCoroutines[pos] = StartCoroutine(RegenResourceAfterDelay(pos, data));
            OnRegenerationStarted?.Invoke(pos, data.regenerationTime);
        }

        private IEnumerator RegenResourceAfterDelay(Vector2Int pos, ResourceDataSO data)
        {
            yield return new WaitForSeconds(data.regenerationTime);

            //Elimino risorsa di regen
            _activeResources.TryGetValue(pos, out GameObject go);
            _gridManager.occupiedTiles.Remove(pos);
            _activeResources.Remove(pos);
            Destroy(go);

            //Rigenero la risorsa nella posizione originaria
            PlaceResourceAt(pos, data);
            
            // Notifica agli osservatori che la risorsa è stata rigenerata
            OnResourceRegenerated?.Invoke(pos, data.resourceType);
            Debug.Log($"--> Resource {data.name} regenerated at {pos}");
            
            // Cleanup della coroutine terminata
            _regenerationCoroutines.Remove(pos);
        }

        private void OnDestroy()
        {
            // Ferma tutte le rigenerazioni attive
            foreach (var coroutine in _regenerationCoroutines.Values)
            {
                StopCoroutine(coroutine);
            }

            _regenerationCoroutines.Clear();
        }

        #region editor

        [ContextMenu("Remove All Resources")]
        private void RemoveAllResources()
        {
            foreach (var resource in _activeResources)
            {
                //Libero la cella occupata 
                _gridManager.occupiedTiles.Remove(resource.Key);

                // Distruggo il GameObject
                Destroy(resource.Value);
            }

            _activeResources.Clear();
            Debug.Log("All resources have been removed.");
        }

        [ContextMenu("Regenerate All Resources")]
        private void RegenerateAllResources()
        {
            RemoveAllResources(); // Elimina tutte le risorse
            GenerateAllResources(); // Rigenera tutte le risorse
            Debug.Log("All resources have been regenerated.");
        }

        #endregion editor
    }
}