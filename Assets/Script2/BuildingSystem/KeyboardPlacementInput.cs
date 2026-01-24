﻿using UnityEngine;
using VContainer;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Gestisce l'input da tastiera per il sistema di piazzamento edifici.
    /// REFACTORED: Usa Dependency Injection (VContainer) invece di FindFirstObjectByType.
    /// Separato dalla logica di placement per aderire al Single Responsibility Principle.
    /// In futuro può essere sostituito con un sistema UI-based.
    /// </summary>
    public sealed class KeyboardPlacementInput : MonoBehaviour
    {
        private BuildingPlacer _placer;
        
        [SerializeField] private BuildingConfigSO _testBuildingConfig;

        [Inject]
        public void Construct(BuildingPlacer placer)
        {
            _placer = placer;
            Debug.Log($"[KeyboardPlacementInput] Construct() called. BuildingPlacer: {(_placer != null ? "✅" : "❌ NULL")}");
        }

        private void Update()
        {
            if (_placer == null)
            {
                if (Time.frameCount % 120 == 0) // Log ogni 2 secondi
                {
                    Debug.LogError("[KeyboardPlacementInput] BuildingPlacer è NULL! VContainer non ha iniettato la dipendenza.");
                }
                return;
            }

            // Tasto 1: Inizia/Conferma placement
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("[KeyboardPlacementInput] Tasto 1 premuto!");
                
                if (!_placer.IsPlacing)
                {
                    // Inizia nuovo placement
                    if (_testBuildingConfig != null)
                    {
                        Debug.Log($"[KeyboardPlacementInput] Chiamando StartPlacing con {_testBuildingConfig.name}");
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
                    Debug.Log("[KeyboardPlacementInput] Chiamando ConfirmPlacement");
                    _placer.ConfirmPlacement();
                }
            }

            // Tasto ESC: Annulla placement
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[KeyboardPlacementInput] Tasto ESC premuto!");
                
                if (_placer.IsPlacing)
                {
                    _placer.CancelPlacement();
                }
            }
        }
    }
}
