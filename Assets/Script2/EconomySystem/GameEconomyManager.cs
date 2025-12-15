﻿using System;
using System.Collections.Generic;
using System.Linq;
using Script2.ResourceSystem.Enums;
using UnityEngine;

namespace Script2.Economy
{
    /// <summary>
    /// Gestisce tutte le risorse del gioco con pattern Singleton thread-safe.
    /// Notifica i cambiamenti tramite eventi.
    /// </summary>
    public class GameEconomyManager : MonoBehaviour
    {
        public static GameEconomyManager Instance { get; private set; }

        public event Action<ResourceType, int> OnResourceAmountChanged;
        public event Action<IReadOnlyDictionary<ResourceType, int>> OnResourcesBatchChanged;
        
        private Dictionary<ResourceType, int> _resources = new();

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Debug.LogWarning("GameEconomyManager duplicato rilevato e distrutto.");
                Destroy(gameObject);
                return;
            }

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!_resources.ContainsKey(type)) _resources[type] = 0;
            }

            Debug.Log("GameEconomyManager Initialized. Current Resources:");
            foreach (var resource in _resources)
            {
                Debug.Log($"- {resource.Key.ToString()}: {resource.Value}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            // Cleanup eventi per prevenire memory leak
            OnResourceAmountChanged = null;
            OnResourcesBatchChanged = null;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempted to add negative amount of {type}. Use SpendResources instead.");
                return;
            }

            _resources[type] = GetResourceAmount(type) + amount;
            OnResourceAmountChanged?.Invoke(type, _resources[type]);
        }

        public bool SpendResources(ResourceType type, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempted to spend negative amount of {type}. Use AddResource instead.");
                return false;
            }

            if (_resources.ContainsKey(type) && _resources[type] >= amount)
            {
                _resources[type] -= amount;
                OnResourceAmountChanged?.Invoke(type, _resources[type]);
                return true;
            }

            Debug.LogWarning($"Not enough {type} to spend {amount}. Current: {_resources.GetValueOrDefault(type, 0)}");
            return false;
        }

        public bool SpendResources(Dictionary<ResourceType, int> costs)
        {
            if (CanAfford(costs))
            {
                foreach (var cost in costs)
                {
                    _resources[cost.Key] = GetResourceAmount(cost.Key) - cost.Value;
                    OnResourceAmountChanged?.Invoke(cost.Key, _resources[cost.Key]);
                }
                OnResourcesBatchChanged?.Invoke(GetResourcesSnapshot());
                return true;
            }

            Debug.LogWarning("Cannot afford purchase. Not enough resources.");
            return false;
        }

        public bool TrySpendResources(Dictionary<ResourceType, int> costs, out Dictionary<ResourceType, int> newBalances)
        {
            newBalances = null;
            if (!CanAfford(costs)) return false;

            foreach (var cost in costs)
            {
                _resources[cost.Key] = GetResourceAmount(cost.Key) - cost.Value;
                OnResourceAmountChanged?.Invoke(cost.Key, _resources[cost.Key]);
            }
            var snapshot = GetResourcesSnapshot();
            OnResourcesBatchChanged?.Invoke(snapshot);
            newBalances = new Dictionary<ResourceType, int>(snapshot);
            return true;
        }

        public int GetResourceAmount(ResourceType type)
        {
            return _resources.GetValueOrDefault(type, 0);
        }

        public IReadOnlyDictionary<ResourceType, int> GetResourcesSnapshot()
        {
            return new Dictionary<ResourceType, int>(_resources);
        }

        public void SetResource(ResourceType type, int amount)
        {
            _resources[type] = Mathf.Max(0, amount);
            OnResourceAmountChanged?.Invoke(type, _resources[type]);
            OnResourcesBatchChanged?.Invoke(GetResourcesSnapshot());
        }

        private bool CanAfford(ResourceType type, int amount)
        {
            if (amount < 0) return true;
            return _resources.ContainsKey(type) && _resources[type] >= amount;
        }

        public bool CanAfford(Dictionary<ResourceType, int> costs)
        {
            return costs == null || costs.All(cost => CanAfford(cost.Key, cost.Value));
        }
    }
}