﻿using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Rappresenta un edificio piazzato nella scena.
    /// Gestisce sprite rendering e sorting per visualizzazione isometrica corretta.
    /// </summary>
    public sealed class Building : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        /// <summary>
        /// Configurazione dell'edificio (immutabile dopo Init).
        /// </summary>
        public BuildingConfigSO Config { get; private set; }

        /// <summary>
        /// Inizializza l'edificio con una configurazione specifica.
        /// Configura rendering e sorting layer.
        /// </summary>
        /// <param name="config">Configurazione dell'edificio</param>
        public void Init(BuildingConfigSO config)
        {
            if (config == null)
            {
                Debug.LogError("[Building] Init chiamato con config null!");
                return;
            }

            Config = config;
            
            // Auto-trova renderer se non assegnato
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_renderer != null)
            {
                _renderer.sortingLayerName = config.SortingLayer;
                _renderer.sortingOrder = config.BaseSortingOrder;
            }
            else
            {
                Debug.LogWarning($"[Building] SpriteRenderer mancante sul prefab: {name}");
            }
        }

        /// <summary>
        /// Aggiorna il sorting order in base alla posizione Y per la visualizzazione isometrica.
        /// Gli oggetti più in basso (Y minore) vengono renderizzati sopra.
        /// </summary>
        /// <param name="yBase">Posizione Y base per il calcolo</param>
        public void SetSortingByY(float yBase)
        {
            if (_renderer == null) return;
            
            _renderer.sortingOrder = Mathf.RoundToInt(-yBase * 100f) + Config.BaseSortingOrder;
        }

        private void OnDestroy()
        {
            // Notifica distruzione edificio
            BuildingEvents.OnBuildingDestroyed?.Invoke(this);
        }
    }
}
