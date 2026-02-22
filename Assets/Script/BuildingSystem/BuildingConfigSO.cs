using System.Collections.Generic;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.BuildingSystem
{
    /// <summary>
    /// ScriptableObject che definisce la configurazione di un edificio.
    /// Approccio data-driven per definire tipologie di edifici senza modificare codice.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/BuildingDataSO", fileName = "BuildingConfig")]
    public class BuildingConfigSO : ScriptableObject
    {
        [Header("Prefab e Visuale")]
        [Tooltip("Prefab dell'edificio da istanziare")]
        public GameObject Prefab;

        [Tooltip("Sorting layer per il rendering")]
        public string SortingLayer = "OnTiles";

        [Tooltip("Sorting order base (verrà modificato in base alla posizione Y per l'isometria)")]
        public int BaseSortingOrder;

        [Header("Dimensioni sulla griglia (celle)")]
        [Tooltip("Larghezza in celle (asse X)")]
        public int Width = 1;

        [Tooltip("Altezza in celle (asse Y)")]
        public int Height = 1;

        /// <summary>
        /// Struttura per definire un costo in risorse.
        /// </summary>
        [System.Serializable]
        public struct ResourceCost
        {
            public ResourceType Type;
            public int Amount;
        }

        [Header("Costo risorse")]
        [Tooltip("Lista dei costi in risorse per costruire questo edificio")]
        public List<ResourceCost> Costs = new();
        
        /// <summary>
        /// Converte la lista di costi in un dizionario per utilizzo interno.
        /// Aggrega costi duplicati dello stesso tipo di risorsa.
        /// </summary>
        /// <returns>Dizionario ResourceType → Amount</returns>
        public Dictionary<ResourceType, int> ToDictionary()
        {
            var dict = new Dictionary<ResourceType, int>();

            foreach (var cost in Costs)
            {
                if (cost.Amount > 0)
                {
                    if (dict.ContainsKey(cost.Type))
                    {
                        dict[cost.Type] += cost.Amount;
                    }
                    else
                    {
                        dict[cost.Type] = cost.Amount;
                    }
                }
            }

            return dict;
        }
    }
}
