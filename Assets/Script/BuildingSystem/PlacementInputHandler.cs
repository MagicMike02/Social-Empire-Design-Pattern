﻿using UnityEngine;
using VContainer;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Gestisce l'input da tastiera per il sistema di piazzamento edifici.
    /// - Usa API event-driven di BuildingPlacer (compatibile con FSM)
    /// - TEMPORANEO: Verrà sostituito con input Mouse + UI (questo è solo per testing)
    /// </summary>
    public sealed class PlacementInputHandler : MonoBehaviour
    {
        private BuildingPlacer _placer;
        private bool _loggedMissingDependency = false;
        
        [SerializeField] private BuildingConfigSO _testBuildingConfig;

        [Inject]
        public void Construct(BuildingPlacer placer)
        {
            _placer = placer;
        }

        private void Update()
        {
            if (_placer == null)
            {
                if (!_loggedMissingDependency)
                {
                    Debug.LogError("[PlacementInputHandler] BuildingPlacer è NULL! VContainer non ha iniettato la dipendenza.");
                    _loggedMissingDependency = true;
                }
                return;
            }

            // ========== KEYBOARD INPUT (TEMPORANEO) ==========
            
            // Tasto 1: Seleziona edificio
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (_testBuildingConfig != null)
                {
                    _placer.SelectBuilding(_testBuildingConfig);
                }
                else
                {
                    Debug.LogWarning("[KeyboardPlacementInput] Test Building Config non assegnato!");
                }
            }

            // Tasto ESC: Cancella placement
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _placer.CancelPlacement();
            }
            
            // ========== MOUSE INPUT (DA IMPLEMENTARE) ==========
            // TODO: Spostare in MousePlacementInput.cs quando UI è pronta
            
            // Left Click: Conferma placement
            if (Input.GetMouseButtonDown(0) && _placer.IsPlacing)
            {
                _placer.ConfirmPlacement();
            }
            
            // Right Click: Cancella placement
            if (Input.GetMouseButtonDown(1) && _placer.IsPlacing)
            {
                _placer.CancelPlacement();
            }
        }
    }
}
