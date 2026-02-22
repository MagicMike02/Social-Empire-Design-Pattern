using System;
using System.Collections.Generic;
using Script.Core.Events;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.EconomySystem
{
    /// <summary>
    /// Gestisce tutte le risorse del gioco.
    /// Notifica i cambiamenti tramite GlobalEventBus.
    /// </summary>
    public class GameEconomyManager : MonoBehaviour
    {
        #region Private Fields
        
        private Dictionary<ResourceType, int> _resources = new();
        
        #endregion

        #region Unity Lifecycle
        
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
        
        #endregion

        #region Public Operations
        
        /// <summary>
        /// Aggiunge una quantita' specifica di una risorsa all'economia.
        /// </summary>
        /// <param name="type">Il tipo di risorsa da aggiungere.</param>
        /// <param name="amount">La quantita' da aggiungere.</param>
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

        /// <summary>
        /// Spende una singola risorsa, se disponibile.
        /// </summary>
        /// <param name="type">Il tipo di risorsa da spendere.</param>
        /// <param name="amount">La quantita' da spendere.</param>
        /// <returns>True se la spesa ha avuto successo, False se non ci sono abbastanza risorse.</returns>
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

        /// <summary>
        /// Spende una serie di costi (Dizionario), se possiede i fondi sufficienti.
        /// Genera eventi GlobalEventBus per ogni risorsa spesa, oltre a un evento batch.
        /// </summary>
        /// <param name="costs">I costi da spendere.</param>
        /// <returns>True se ha fondi completi per tutte le chiavi, False altrimenti.</returns>
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

        /// <summary>
        /// Tenta di spendere risorse e restituisce i nuovi saldi aggiornati.
        /// </summary>
        /// <param name="costs">I costi richiesti.</param>
        /// <param name="newBalances">I saldi calcolati e aggiornati.</param>
        /// <returns>True se l'operazione va a buon fine.</returns>
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

        #endregion

        #region Queries and Setters
        
        /// <summary>
        /// Ottiene la quantita' immagazzinata per una determinata risorsa.
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            return _resources.GetValueOrDefault(type, 0);
        }

        /// <summary>
        /// Restituisce uno snapshot thread-safe in sola lettura.
        /// </summary>
        public IReadOnlyDictionary<ResourceType, int> GetResourcesSnapshot()
        {
            return new Dictionary<ResourceType, int>(_resources);
        }

        /// <summary>
        /// Imposta forzatamente il valore di una risorsa, calcolando i delta.
        /// </summary>
        public void SetResource(ResourceType type, int amount)
        {
            int previousAmount = _resources.GetValueOrDefault(type, 0);
            _resources[type] = Mathf.Max(0, amount);
            int delta = _resources[type] - previousAmount;
            
            // Pubblica entrambi gli eventi
            GlobalEventBus.Publish(new ResourceAmountChangedEvent(type, _resources[type], delta));
            GlobalEventBus.Publish(new ResourcesBatchChangedEvent(GetResourcesSnapshot()));
        }

        /// <summary>
        /// Controlla se il giocatore puo' permettersi una specifica spesa singola.
        /// </summary>
        private bool CanAfford(ResourceType type, int amount)
        {
            if (amount < 0) return true;
            return _resources.ContainsKey(type) && _resources[type] >= amount;
        }

        /// <summary>
        /// Controlla se il giocatore dispone di tutti i materiali richiesti.
        /// ZERO ALLOCATIONS: Sostituisce LINQ All() per evitare garbage.
        /// </summary>
        public bool CanAfford(Dictionary<ResourceType, int> costs)
        {
            if (costs == null) return true;
            
            foreach (var kvp in costs)
            {
                if (!CanAfford(kvp.Key, kvp.Value))
                    return false;
            }
            return true;
        }

        #endregion
    }
}