using System.Collections.Generic;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.GridSystem
{
    [System.Serializable]
    public struct ResourceCost
    {
        public ResourceType resourceType;
        public int amount;
    }

    [System.Serializable]
    public struct ZoneExpansionLevel
    {
        [Tooltip("Costi necessari per sbloccare l'espansione a questo livello.")]
        public List<ResourceCost> costs;
    }

    /// <summary>
    /// Configurazione dati per gestire i costi di espansione della mappa (Zone) in modo data-driven.
    /// I costi scalano in base al numero di zone già sbloccate.
    /// </summary>
    [CreateAssetMenu(fileName = "NewZoneExpansionData", menuName = "ScriptableObjects/ZoneExpansionDataSO")]
    public class ZoneExpansionDataSO : ScriptableObject
    {
        [Header("Expansion Costs per Level")]
        [Tooltip("Lista dei costi per ogni livello di espansione. L'indice dell'array corrisponde al numero di zone già sbloccate.")]
        public List<ZoneExpansionLevel> expansionLevels;

        [Header("Fallback Settings")]
        [Tooltip("Se il player sblocca più zone di quelle configurate negli expansionLevels, usa questa formula per calcolare il costo aggiuntivo.")]
        public bool useFormulaFallback = true;
        
        [Tooltip("Costo base per il calcolo della formula di fallback.")]
        public ResourceCost baseFallbackCost = new ResourceCost { resourceType = ResourceType.Gold, amount = 1000 };
        
        [Tooltip("Moltiplicatore applicato per ogni zona oltre il livello massimo configurato.")]
        public float fallbackMultiplierPerLevel = 1.5f;

        /// <summary>
        /// Restituisce la lista di costi richiesti per sbloccare la prossima zona, 
        /// dato il numero di zone che il giocatore ha già sbloccato.
        /// </summary>
        public Dictionary<ResourceType, int> GetCostForNextZone(int currentlyUnlockedZonesCount)
        {
            var costDict = new Dictionary<ResourceType, int>();

            // Se abbiamo espansioni configurate per questo livello, usiamole
            if (expansionLevels != null && currentlyUnlockedZonesCount < expansionLevels.Count)
            {
                var levelData = expansionLevels[currentlyUnlockedZonesCount];
                foreach (var cost in levelData.costs)
                {
                    if (costDict.ContainsKey(cost.resourceType))
                        costDict[cost.resourceType] += cost.amount;
                    else
                        costDict[cost.resourceType] = cost.amount;
                }
                return costDict;
            }

            // Fallback: se le zone configurate finiscono e la formula è attiva
            if (useFormulaFallback)
            {
                // Calcola quanti "livelli extra" ha sbloccato oltre il massimo configurato
                int maxConfiguredLevels = expansionLevels?.Count ?? 0;
                int extraLevels = currentlyUnlockedZonesCount - maxConfiguredLevels;
                
                int calculatedAmount = Mathf.RoundToInt(baseFallbackCost.amount * Mathf.Pow(fallbackMultiplierPerLevel, extraLevels));
                
                costDict[baseFallbackCost.resourceType] = calculatedAmount;
            }
            
            return costDict;
        }
    }
}
