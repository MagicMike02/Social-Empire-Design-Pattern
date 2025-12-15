﻿using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Rappresenta l'anteprima visuale di un edificio durante il piazzamento.
    /// Mostra un feedback visivo (colore verde/rosso) in base alla validità della posizione.
    /// </summary>
    public sealed class BuildingGhost : MonoBehaviour
    {
        #region Fields
        
        [SerializeField] private SpriteRenderer _renderer;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Configurazione dell'edificio in anteprima.
        /// </summary>
        public BuildingConfigSO Config { get; private set; }
        
        /// <summary>
        /// Indica se la posizione corrente è valida per il piazzamento.
        /// </summary>
        public bool IsValid { get; private set; }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Inizializza il ghost con una configurazione edificio.
        /// </summary>
        /// <param name="config">Configurazione da utilizzare</param>
        public void Init(BuildingConfigSO config)
        {
            if (config == null)
            {
                Debug.LogError("[BuildingGhost] Init chiamato con config null!");
                return;
            }

            Config = config;
            
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_renderer != null)
            {
                _renderer.sortingLayerName = config.SortingLayer;
            }
            else
            {
                Debug.LogWarning("[BuildingGhost] SpriteRenderer non trovato. La preview potrebbe non essere visibile.");
            }
        }

        /// <summary>
        /// Aggiorna l'aspetto visivo del ghost in base alla validità della posizione.
        /// </summary>
        /// <param name="isValid">True se la posizione è valida, False altrimenti</param>
        /// <param name="yPosition">Posizione Y per calcolare il sorting order isometrico</param>
        public void UpdateVisual(bool isValid, float yPosition)
        {
            IsValid = isValid;
            
            if (_renderer == null || Config == null)
            {
                return;
            }

            // Aggiorna sorting isometrico
            SortingUtils.ApplySorting(_renderer, Config.SortingLayer, yPosition, Config.BaseSortingOrder);
            
            // Applica colore (verde se valido, rosso se invalido)
            var targetColor = isValid ? Config.ValidColor : Config.InvalidColor;
            SortingUtils.SetGhostColor(_renderer, targetColor, Config.GhostAlpha);
        }
        
        #endregion
    }
}
