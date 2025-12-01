using System;
using System.Collections.Generic;
using Script2.GridSystem;
using Script2.ResourceSystem.Enums;
using UnityEngine;

namespace Script2.Economy
{
    public class GameEconomyManager : MonoBehaviour
    {
        public static GameEconomyManager Instance { get; private set; }

        public event Action<ResourceType, int> OnResourceAmountChanged;
        private Dictionary<ResourceType, int> _resources = new();

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Destroy(gameObject);
            }

            // Inizializza tutte le risorse a 0
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                _resources[type] = 0;
            }

            Debug.Log("GameEconomyManager Initialized. Current Resources:");
            foreach (var resource in _resources)
            {
                Debug.Log($"- {resource.Key}: {resource.Value}");
            }
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempted to add negative amount of {type}. Use SpendResources instead.");
                return;
            }

            _resources[type] += amount;
            Debug.Log($"Added {amount} {type}. Total: {_resources[type]}");
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
                Debug.Log($"Spent {amount} {type}. Total: {_resources[type]}");
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
                    _resources[cost.Key] -= cost.Value;
                    OnResourceAmountChanged?.Invoke(cost.Key, _resources[cost.Key]);
                }

                Debug.Log($"Cost Resources {costs.Keys} : {costs.Values} spent successfully for purchase.");
                return true;
            }

            Debug.LogWarning("Cannot afford purchase. Not enough resources.");
            return false;
        }

        public int GetResourceAmount(ResourceType type)
        {
            return _resources.GetValueOrDefault(type, 0);
        }

        public bool CanAfford(ResourceType type, int amount)
        {
            if (amount < 0) return true;
            return _resources.ContainsKey(type) && _resources[type] >= amount;
        }

        public bool CanAfford(Dictionary<ResourceType, int> costs)
        {
			if (costs == null) return true;
			foreach (var cost in costs)
			{
				if (!CanAfford(cost.Key, cost.Value))
					return false;
			}
			return true;
		}
    }
}