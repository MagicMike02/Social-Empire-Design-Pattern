﻿using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Gestisce l'input da tastiera per il sistema di piazzamento edifici.
    /// Separato dalla logica di placement per aderire al Single Responsibility Principle.
    /// In futuro può essere sostituito con un sistema UI-based.
    /// </summary>
    public sealed class KeyboardPlacementInput : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer _placer;
        [SerializeField] private BuildingConfigSO _testBuildingConfig;

        private void Awake()
        {
            if (_placer == null)
            {
                _placer = FindFirstObjectByType<BuildingPlacer>();
            }
            
            if (_placer == null)
            {
                Debug.LogError("[KeyboardPlacementInput] BuildingPlacer non trovato in scena! Questo componente non funzionerà.");
            }
        }

        private void Update()
        {
            if (_placer == null) return;

            // Tasto 1: Inizia/Conferma placement
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (!_placer.IsPlacing)
                {
                    // Inizia nuovo placement
                    if (_testBuildingConfig != null)
                    {
                        _placer.StartPlacing(_testBuildingConfig);
                    }
                    else
                    {
                        Debug.LogWarning("[KeyboardPlacementInput] Test Building Config non assegnato!");
                    }
                }
                else
                {
                    // Conferma placement in corso
                    _placer.ConfirmPlacement();
                }
            }

            // Tasto ESC: Annulla placement
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_placer.IsPlacing)
                {
                    _placer.CancelPlacement();
                }
            }
        }
    }
}
