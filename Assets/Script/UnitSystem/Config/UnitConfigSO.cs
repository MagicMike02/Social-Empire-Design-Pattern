using UnityEngine;
using System;
using System.Collections.Generic;
using Script.ResourceSystem.Enums;

namespace Script.UnitSystem.Config
{
    public enum UnitRole
    {
        Worker,
        Infantry,
        Ranged,
        Tank,
        Support,
        Hero
    }

    /// <summary>
    /// Configurazione data-driven della singola unita'.
    /// Modello simile a RTS city-builder: identita', produzione e progressione per livello.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/UnitSystem/UnitConfig", fileName = "UnitConfig")]
    public sealed class UnitConfigSO : ScriptableObject
    {
        [Serializable]
        public struct ResourceCost
        {
            public ResourceType Resource;
            public int Amount;
        }

        [Serializable]
        public struct UnitLevelData
        {
            public int Level;
            public int HitPoints;
            public float MoveSpeed;
            public float CellAcquireRetrySeconds;
            public float CellAcquireTimeoutSeconds;
            public int Attack;
            public float AttackRange;
        }

        [Header("Identity")]
        [SerializeField] private string _unitId = "unit";
        [SerializeField] private string _displayName = "Unit";
        [SerializeField] private UnitRole _role = UnitRole.Infantry;
        [SerializeField] private GameObject _prefab;

        [Header("Production")]
        [SerializeField] private float _trainingTimeSeconds = 5f;
        [SerializeField] private int _populationCost = 1;
        [SerializeField] private List<ResourceCost> _trainingCost = new();

        [Header("Progression")]
        [SerializeField] private int _defaultLevel = 1;
        [SerializeField] private List<UnitLevelData> _levels = new();

        public string UnitId => _unitId;
        public string DisplayName => _displayName;
        public UnitRole Role => _role;
        public GameObject Prefab => _prefab;
        public float TrainingTimeSeconds => _trainingTimeSeconds;
        public int PopulationCost => _populationCost;
        public int DefaultLevel => _defaultLevel;
        public IReadOnlyList<ResourceCost> TrainingCost => _trainingCost;
        public IReadOnlyList<UnitLevelData> Levels => _levels;

        // Compatibility accessors for M1 movement-only pipeline.
        public float MoveSpeed => GetLevelData(_defaultLevel).MoveSpeed;
        public float CellAcquireRetrySeconds => GetLevelData(_defaultLevel).CellAcquireRetrySeconds;
        public float CellAcquireTimeoutSeconds => GetLevelData(_defaultLevel).CellAcquireTimeoutSeconds;

        public UnitLevelData GetLevelData(int level)
        {
            if (_levels == null || _levels.Count == 0)
            {
                return new UnitLevelData
                {
                    Level = Mathf.Max(1, level),
                    HitPoints = 100,
                    MoveSpeed = 3.5f,
                    CellAcquireRetrySeconds = 0.15f,
                    CellAcquireTimeoutSeconds = 1.2f,
                    Attack = 10,
                    AttackRange = 1f,
                };
            }

            for (int i = 0; i < _levels.Count; i++)
            {
                if (_levels[i].Level == level)
                {
                    return _levels[i];
                }
            }

            return _levels[0];
        }

        public Dictionary<ResourceType, int> GetTrainingCostDictionary()
        {
            var dict = new Dictionary<ResourceType, int>();
            if (_trainingCost == null) return dict;

            for (int i = 0; i < _trainingCost.Count; i++)
            {
                var cost = _trainingCost[i];
                if (cost.Amount <= 0 || cost.Resource == ResourceType.None) continue;

                if (!dict.ContainsKey(cost.Resource))
                {
                    dict[cost.Resource] = 0;
                }

                dict[cost.Resource] += cost.Amount;
            }

            return dict;
        }

        private void OnValidate()
        {
            _defaultLevel = Mathf.Max(1, _defaultLevel);
            _trainingTimeSeconds = Mathf.Max(0.1f, _trainingTimeSeconds);
            _populationCost = Mathf.Max(0, _populationCost);

            if (_levels == null)
            {
                return;
            }

            for (int i = 0; i < _levels.Count; i++)
            {
                var item = _levels[i];
                item.Level = Mathf.Max(1, item.Level);
                item.HitPoints = Mathf.Max(1, item.HitPoints);
                item.MoveSpeed = Mathf.Max(0.1f, item.MoveSpeed);
                item.CellAcquireRetrySeconds = Mathf.Max(0.01f, item.CellAcquireRetrySeconds);
                item.CellAcquireTimeoutSeconds = Mathf.Max(item.CellAcquireRetrySeconds, item.CellAcquireTimeoutSeconds);
                item.Attack = Mathf.Max(0, item.Attack);
                item.AttackRange = Mathf.Max(0.1f, item.AttackRange);
                _levels[i] = item;
            }
        }
    }
}
