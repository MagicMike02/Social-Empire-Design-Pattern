﻿using UnityEngine;
using VContainer;
using VContainer.Unity;
using Script2.BuildingSystem;
using Script2.Economy;
using Script2.GridSystem;
using Script2.ResourceSystem;
using Script2.Common;
using Script2.InputSystem;
using Script2.PathfindingSystem;

namespace Script2.Core
{
    /// <summary>
    /// VContainer LifetimeScope - IMPLEMENTAZIONE CORRETTA.
    /// </summary>
    [DefaultExecutionOrder(-10000)] // Esegui PRIMA di tutti gli altri MonoBehaviour
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private bool debugMode = true;

        protected override void Awake()
        {
            // CRITICO: Forza AutoRun = true
            autoRun = true;

            try
            {
                base.Awake();
                if (debugMode) Debug.Log("[GameLifetimeScope] ✓ Inizializzato");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameLifetimeScope] ERRORE durante l'inizializzazione: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (debugMode) Debug.Log("[GameLifetimeScope] Configurazione dei servizi in corso...");

            // CORE - Economy
            RegisterIfExists<GameEconomyManager>(builder);
            
            // GRID SYSTEM
            RegisterIfExists<TileManager>(builder);
            RegisterIfExists<ZoneManager>(builder);
            RegisterIfExists<GridManager>(builder, r => r.As<IGridService>());
            
            // RESOURCE SYSTEM
            RegisterIfExists<ResourceSpawner>(builder);
            RegisterIfExists<ResourceManager>(builder);
            
            // BUILDING SYSTEM
            RegisterIfExists<BuildingFactory>(builder);
            RegisterIfExists<BuildingManager>(builder);
            RegisterIfExists<BuildingEventBus>(builder);
            RegisterIfExists<GenericPreviewSystem>(builder);
            RegisterIfExists<BuildingPlacer>(builder);
            RegisterIfExists<KeyboardPlacementInput>(builder);
            
            // INPUT SYSTEM
            RegisterIfExists<InputManager>(builder);
            
            // PATHFINDING SYSTEM (SPRINT 1)
            RegisterIfExists<PathfindingManager>(builder);
            
            // CAMERA
            var camera = Camera.main;
            if (camera != null)
            {
                builder.RegisterInstance(camera);
                if (debugMode) Debug.Log("[GameLifetimeScope] ✓ Camera registrata");
            }
            else
            {
                Debug.LogWarning("[GameLifetimeScope] Camera.main non trovata!");
            }

            if (debugMode) Debug.Log("[GameLifetimeScope] ✓ Configurazione completata");
        }

        private void RegisterIfExists<T>(IContainerBuilder builder, System.Action<RegistrationBuilder> configure = null) where T : Component
        {
            var component = FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
            
            if (component != null)
            {
                var registration = builder.RegisterComponent(component);
                configure?.Invoke(registration);
                if (debugMode) Debug.Log($"[GameLifetimeScope] ✓ {typeof(T).Name}");
            }
            else
            {
                Debug.LogError($"[GameLifetimeScope] ✗ {typeof(T).Name} non trovato in scena!");
            }
        }
    }
}
