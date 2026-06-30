using System.Collections.Generic;
using Script.UnitSystem.Config;
using UnityEngine;
using VContainer;

namespace Script.UnitSystem.Production
{
    /// <summary>
    /// Gestisce reward unit da eventi/missioni e spawn opzionale immediato.
    /// </summary>
    public sealed class UnitRewardGrantService : MonoBehaviour
    {
        private readonly Dictionary<string, int> _reservedUnits = new();

        private UnitUnlockService _unlockService;
        private UnitSpawnService _spawnService;

        [SerializeField] private UnitSystemConfigSO _config;

        [Inject]
        public void Construct(UnitUnlockService unlockService, UnitSpawnService spawnService)
        {
            _unlockService = unlockService;
            _spawnService = spawnService;
        }

        public bool GrantReward(string unitId, int count, bool spawnImmediately, Vector3 worldPosition)
        {
            if (string.IsNullOrWhiteSpace(unitId) || count <= 0) return false;
            if (_config == null || _config.UnitCatalog == null) return false;
            if (!_config.UnitCatalog.TryGetById(unitId, out var unitConfig) || unitConfig == null) return false;

            _unlockService?.Unlock(unitId);

            if (spawnImmediately && _spawnService != null)
            {
                var request = new UnitSpawnRequest
                {
                    UnitId = unitId,
                    Level = _config.DefaultUnitLevel,
                    Count = count,
                    Source = UnitSpawnSourceType.Reward,
                    UseWorldPosition = true,
                    WorldPosition = worldPosition,
                    SearchRadius = 10,
                    IgnoreUnlock = true,
                    ParentOverride = null,
                };

                return _spawnService.TrySpawnUnits(request, out _);
            }

            if (!_reservedUnits.ContainsKey(unitId))
            {
                _reservedUnits[unitId] = 0;
            }

            _reservedUnits[unitId] += count;
            return true;
        }

        public int GetReservedCount(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return 0;
            return _reservedUnits.TryGetValue(unitId, out var amount) ? amount : 0;
        }

        public bool ConsumeReserved(string unitId, int amount, Vector3 spawnWorldPosition)
        {
            if (string.IsNullOrWhiteSpace(unitId) || amount <= 0) return false;
            if (!_reservedUnits.TryGetValue(unitId, out var available) || available < amount) return false;
            if (_spawnService == null) return false;

            var request = new UnitSpawnRequest
            {
                UnitId = unitId,
                Level = _config != null ? _config.DefaultUnitLevel : 1,
                Count = amount,
                Source = UnitSpawnSourceType.Reward,
                UseWorldPosition = true,
                WorldPosition = spawnWorldPosition,
                SearchRadius = 10,
                IgnoreUnlock = true,
                ParentOverride = null,
            };

            if (!_spawnService.TrySpawnUnits(request, out var spawned))
            {
                return false;
            }

            _reservedUnits[unitId] = Mathf.Max(0, available - spawned);
            return spawned > 0;
        }
    }
}
