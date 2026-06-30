using System.Collections.Generic;
using Script.BuildingSystem;
using Script.EconomySystem;
using Script.UnitSystem.Config;
using UnityEngine;
using VContainer;

namespace Script.UnitSystem.Production
{
    /// <summary>
    /// Coda produzione unita' per sorgente edificio (training queue + timer + spawn).
    /// </summary>
    public sealed class UnitProductionQueueService : MonoBehaviour
    {
        private sealed class QueueEntry
        {
            public string UnitId;
            public int Level;
            public float RemainingSeconds;
            public Building SourceBuilding;
            public Dictionary<ResourceSystem.Enums.ResourceType, int> TrainingCost;
        }

        private sealed class BuildingQueueState
        {
            public readonly Queue<QueueEntry> Pending = new();
            public QueueEntry Active;
        }

        private readonly Dictionary<int, BuildingQueueState> _queuesByBuilding = new();

        private UnitUnlockService _unlockService;
        private UnitSpawnService _spawnService;
        private GameEconomyManager _economy;

        [SerializeField] private UnitSystemConfigSO _config;

        [Inject]
        public void Construct(UnitUnlockService unlockService, UnitSpawnService spawnService, GameEconomyManager economy)
        {
            _unlockService = unlockService;
            _spawnService = spawnService;
            _economy = economy;
        }

        private void Update()
        {
            if (_queuesByBuilding.Count == 0) return;

            var toRemove = new List<int>();
            foreach (var kvp in _queuesByBuilding)
            {
                TickBuildingQueue(kvp.Key, kvp.Value, toRemove);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                _queuesByBuilding.Remove(toRemove[i]);
            }
        }

        public bool TryEnqueueFromBuilding(Building building, string unitId, int level = 1)
        {
            if (building == null || string.IsNullOrWhiteSpace(unitId) || _config == null || _config.UnitCatalog == null)
            {
                return false;
            }

            if (_unlockService != null && !_unlockService.IsUnlocked(unitId))
            {
                return false;
            }

            if (!_config.UnitCatalog.TryGetById(unitId, out var unitConfig) || unitConfig == null)
            {
                return false;
            }

            var cost = unitConfig.GetTrainingCostDictionary();
            if (_economy != null && !_economy.SpendResources(cost))
            {
                return false;
            }

            int buildingId = building.GetInstanceID();
            if (!_queuesByBuilding.TryGetValue(buildingId, out var queue))
            {
                queue = new BuildingQueueState();
                _queuesByBuilding.Add(buildingId, queue);
            }

            var entry = new QueueEntry
            {
                UnitId = unitId,
                Level = Mathf.Max(1, level),
                RemainingSeconds = Mathf.Max(0.1f, unitConfig.TrainingTimeSeconds),
                SourceBuilding = building,
                TrainingCost = cost,
            };

            queue.Pending.Enqueue(entry);
            return true;
        }

        public bool TryCancelNext(Building building, bool refund = true)
        {
            if (building == null) return false;

            int id = building.GetInstanceID();
            if (!_queuesByBuilding.TryGetValue(id, out var queue)) return false;
            if (queue.Pending.Count == 0) return false;

            var entry = queue.Pending.Dequeue();

            if (refund && _economy != null && entry.TrainingCost != null)
            {
                foreach (var kvp in entry.TrainingCost)
                {
                    _economy.AddResource(kvp.Key, kvp.Value);
                }
            }

            return true;
        }

        private void TickBuildingQueue(int buildingId, BuildingQueueState queue, List<int> toRemove)
        {
            if (queue == null) return;

            if (queue.Active == null)
            {
                if (queue.Pending.Count == 0) return;
                queue.Active = queue.Pending.Dequeue();
            }

            queue.Active.RemainingSeconds -= Time.deltaTime;
            if (queue.Active.RemainingSeconds > 0f)
            {
                return;
            }

            var entry = queue.Active;
            queue.Active = null;

            if (_spawnService == null || entry.SourceBuilding == null)
            {
                if (queue.Pending.Count == 0)
                {
                    toRemove.Add(buildingId);
                }
                return;
            }

            var request = new UnitSpawnRequest
            {
                UnitId = entry.UnitId,
                Level = entry.Level,
                Count = 1,
                Source = UnitSpawnSourceType.Building,
                UseWorldPosition = true,
                WorldPosition = entry.SourceBuilding.transform.position,
                SearchRadius = 8,
                IgnoreUnlock = false,
                ParentOverride = null,
            };

            _spawnService.TrySpawnUnits(request, out _);

            if (queue.Pending.Count == 0 && queue.Active == null)
            {
                toRemove.Add(buildingId);
            }
        }
    }
}
