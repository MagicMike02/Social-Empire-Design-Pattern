﻿using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Factory per la creazione di istanze di edifici.
    /// Implementa il pattern Factory per centralizzare la logica di creazione.
    /// </summary>
    public sealed class BuildingFactory : MonoBehaviour
    {
        /// <summary>
        /// Crea una nuova istanza di edificio a partire da una configurazione.
        /// </summary>
        /// <param name="config">Configurazione dell'edificio da creare</param>
        /// <param name="worldPos">Posizione world dove istanziare l'edificio</param>
        /// <param name="parent">Transform genitore (opzionale)</param>
        /// <returns>Istanza di Building creata, o null se fallisce</returns>
        public Building CreateBuilding(BuildingConfigSO config, Vector3 worldPos, Transform parent = null)
        {
            if (config == null)
            {
                Debug.LogError("[BuildingFactory] Impossibile creare edificio: config è null!");
                return null;
            }

            if (config.Prefab == null)
            {
                Debug.LogError($"[BuildingFactory] Impossibile creare edificio '{config.name}': Prefab mancante!");
                return null;
            }

            // Istanzia il prefab
            var buildingGO = Instantiate(config.Prefab, worldPos, Quaternion.identity, parent);
            buildingGO.name = $"{config.name}_Instance";

            // Ottieni o aggiungi componente Building
            var building = buildingGO.GetComponent<Building>();
            if (building == null)
            {
                building = buildingGO.AddComponent<Building>();
            }

            // Inizializza building
            building.Init(config);

            // Applica sorting isometrico
            var spriteRenderer = buildingGO.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                IsometricSortingUtility.ApplySorting(spriteRenderer, config.SortingLayer, worldPos.y, config.BaseSortingOrder);
            }
            else
            {
                Debug.LogWarning($"[BuildingFactory] SpriteRenderer non trovato in '{config.name}'. Il sorting potrebbe essere errato.");
            }

            return building;
        }
    }
}

