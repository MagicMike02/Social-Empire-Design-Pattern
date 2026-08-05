﻿using UnityEngine;
using VContainer;
using Script.InputSystem;
using Script.GridSystem;
using UnityEngine.InputSystem;

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
        private InputManager _inputManager;
        private bool _loggedMissingDependency = false;
        
        [Header("Available Buildings")]
        [Tooltip("Map indices to keyboard numbers: 0 -> Key 1, 1 -> Key 2")]
        [SerializeField] private BuildingConfigSO[] _availableBuildings;
        [Header("Input Actions")]
        [Tooltip("Binding atteso: tasti 1..9, un'azione per slot")]
        [SerializeField] private InputActionReference[] _selectBuildingActions = new InputActionReference[9];
        [SerializeField] private InputActionReference _cancelAction;

        [Inject]
        public void Construct(BuildingPlacer placer, InputManager inputManager)
        {
            try
            {
                _placer = placer;
                _inputManager = inputManager;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[PlacementInputHandler] Errore durante Construct: {ex.Message}");
#endif
            }
        }

        private void OnEnable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnTileClicked += HandleTileClicked;
                _inputManager.OnMapRightClicked += HandleRightClicked;
            }

            EnableActions();
        }

        private void OnDisable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnTileClicked -= HandleTileClicked;
                _inputManager.OnMapRightClicked -= HandleRightClicked;
            }

            DisableActions();
        }

        private void HandleTileClicked(Tile tile)
        {
            if (_placer != null && _placer.IsPlacing)
            {
                _placer.ConfirmPlacement();
            }
        }

        private void HandleRightClicked()
        {
            if (_placer != null && _placer.IsPlacing)
            {
                _placer.CancelPlacement();
            }
        }

        private void Update()
        {
            if (_placer == null)
            {
                if (!_loggedMissingDependency)
                {
#if UNITY_EDITOR
                    Debug.LogError("[PlacementInputHandler] BuildingPlacer è NULL! VContainer non ha iniettato la dipendenza.");
#endif
				    _loggedMissingDependency = true;
                }
                return;
            }

            // ========== KEYBOARD INPUT (TEMPORANEO) ==========
            
            // Tasti 1-9: Seleziona edificio dall'array
            if (_availableBuildings != null && _availableBuildings.Length > 0)
            {
                for (int i = 0; i < Mathf.Min(_availableBuildings.Length, 9); i++)
                {
                    var action = i < _selectBuildingActions.Length ? _selectBuildingActions[i] : null;
                    if (action != null && action.action != null && action.action.WasPressedThisFrame())
                    {
                        var config = _availableBuildings[i];
                        if (config != null)
                        {
                            try
                            {
                                _placer.SelectBuilding(config);
#if UNITY_EDITOR							
                                Debug.Log($"[PlacementInputHandler] Selected {config.name} (Key {i + 1})");
#endif
                            }
                            catch (System.Exception ex)
                            {
#if UNITY_EDITOR
                                Debug.LogError($"[PlacementInputHandler] Errore SelectBuilding su '{config.name}' (Key {i + 1}): {ex.Message}");
#endif
                            }
                        }
                    }
                }
            }

            // Tasto ESC: Cancella placement
            if (_cancelAction != null && _cancelAction.action != null && _cancelAction.action.WasPressedThisFrame())
            {
                _placer.CancelPlacement();
            }
        }

        private void EnableActions()
        {
            if (_selectBuildingActions != null)
            {
                for (int i = 0; i < _selectBuildingActions.Length; i++)
                {
                    _selectBuildingActions[i]?.action?.Enable();
                }
            }

            _cancelAction?.action?.Enable();
        }

        private void DisableActions()
        {
            if (_selectBuildingActions != null)
            {
                for (int i = 0; i < _selectBuildingActions.Length; i++)
                {
                    _selectBuildingActions[i]?.action?.Disable();
                }
            }

            _cancelAction?.action?.Disable();
        }
    }
}
