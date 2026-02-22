﻿using UnityEngine;
using VContainer;
using Script.InputSystem;
using Script.GridSystem;

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

        [Inject]
        public void Construct(BuildingPlacer placer, InputManager inputManager)
        {
            _placer = placer;
            _inputManager = inputManager;
        }

        private void OnEnable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnTileClicked += HandleTileClicked;
                _inputManager.OnMapRightClicked += HandleRightClicked;
            }
        }

        private void OnDisable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnTileClicked -= HandleTileClicked;
                _inputManager.OnMapRightClicked -= HandleRightClicked;
            }
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
                    Debug.LogError("[PlacementInputHandler] BuildingPlacer è NULL! VContainer non ha iniettato la dipendenza.");
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
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        var config = _availableBuildings[i];
                        if (config != null)
                        {
                            _placer.SelectBuilding(config);
                            Debug.Log($"[PlacementInputHandler] Selected {config.name} (Key {i + 1})");
                        }
                    }
                }
            }

            // Tasto ESC: Cancella placement
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _placer.CancelPlacement();
            }
        }
    }
}
