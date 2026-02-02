﻿using System.Collections.Generic;
 using Script.ResourceSystem.Enums;
 using UnityEngine;

namespace Script.BuildingSystem
{
    /// <summary>
    /// ScriptableObject che definisce la configurazione di un edificio.
    /// Approccio data-driven per definire tipologie di edifici senza modificare codice.
    /// </summary>
    [CreateAssetMenu(menuName = "Script/Building/Config", fileName = "BuildingConfig")]
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

        [Header("Anteprima")]
        [Tooltip("Colore della preview quando il piazzamento è valido")]
        public Color ValidColor = new Color(0f, 1f, 0f, 0.5f);

        [Tooltip("Colore della preview quando il piazzamento non è valido")]
        public Color InvalidColor = new Color(1f, 0f, 0f, 0.5f);

        [Tooltip("Trasparenza del ghost durante il piazzamento")]
        [Range(0f, 1f)]
        public float GhostAlpha = 0.6f;

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
