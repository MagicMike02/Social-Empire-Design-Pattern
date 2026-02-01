using System;
using System.Collections.Generic;
using System.Linq;
using Script2.ResourceSystem.Enums;
using Script2.Core.Events;
using UnityEngine;

namespace Script2.EconomySystem
{
    /// <summary>
    /// Gestisce tutte le risorse del gioco.
    /// REFACTORED: Usa Dependency Injection invece di Singleton pattern.
    /// Notifica i cambiamenti tramite GlobalEventBus.
    /// </summary>
    public class GameEconomyManager : MonoBehaviour
    {
        private Dictionary<ResourceType, int> _resources = new();

        private void Awake()
        {
            // Inizializza risorse
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!_resources.ContainsKey(type)) _resources[type] = 0;
            }

#if UNITY_EDITOR
            Debug.Log("[GameEconomyManager] Initialized. Current Resources:");
            foreach (var resource in _resources)
            {
                Debug.Log($"  - {resource.Key}: {resource.Value}");
            }
#endif
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempted to add negative amount of {type}. Use SpendResources instead.");
                return;
            }

            int previousAmount = GetResourceAmount(type);
            _resources[type] = previousAmount + amount;
            
            // Pubblica evento GlobalEventBus con delta positivo
            GlobalEventBus.Publish(new ResourceAmountChangedEvent(
                type, 
                _resources[type], 
                amount // delta = amount aggiunto
            ));
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
                
                // Pubblica evento GlobalEventBus con delta negativo
                GlobalEventBus.Publish(new ResourceAmountChangedEvent(
                    type, 
                    _resources[type], 
                    -amount // delta = spesa negativa
                ));
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
                    
                    // Pubblica evento per ogni risorsa spesa
                    GlobalEventBus.Publish(new ResourceAmountChangedEvent(
                        cost.Key, 
                        _resources[cost.Key], 
                        -cost.Value
                    ));
                }
                
                // Pubblica evento batch
                GlobalEventBus.Publish(new ResourcesBatchChangedEvent(GetResourcesSnapshot()));
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
                
                // Pubblica evento per ogni risorsa
                GlobalEventBus.Publish(new ResourceAmountChangedEvent(
                    cost.Key, 
                    _resources[cost.Key], 
                    -cost.Value
                ));
            }
            
            var snapshot = GetResourcesSnapshot();
            GlobalEventBus.Publish(new ResourcesBatchChangedEvent(snapshot));
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
            int previousAmount = _resources.GetValueOrDefault(type, 0);
            _resources[type] = Mathf.Max(0, amount);
            int delta = _resources[type] - previousAmount;
            
            // Pubblica entrambi gli eventi
            GlobalEventBus.Publish(new ResourceAmountChangedEvent(type, _resources[type], delta));
            GlobalEventBus.Publish(new ResourcesBatchChangedEvent(GetResourcesSnapshot()));
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