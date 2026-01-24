﻿using UnityEngine;
using VContainer;
using VContainer.Unity;
using Script2.BuildingSystem;
using Script2.Economy;
using Script2.GridSystem;
using Script2.ResourceSystem;
using Script2.Common;

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
            if (debugMode) Debug.Log("[DI] ==================== AWAKE START ====================");
            
            // CRITICO: Forza AutoRun = true
            autoRun = true;
            
            if (debugMode) Debug.Log($"[DI] AutoRun set to: {autoRun}");
            
            try
            {
                base.Awake();
                if (debugMode) Debug.Log("[DI] base.Awake() completed - Container should be built");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DI] EXCEPTION in Awake(): {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            
            if (debugMode) Debug.Log("[DI] ==================== AWAKE END ====================");
        }

        protected override void Configure(IContainerBuilder builder)
        {
            Debug.Log("[DI] ========== CONFIGURE START ==========");

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
            
            // CAMERA
            var camera = Camera.main;
            if (camera != null)
            {
                builder.RegisterInstance(camera);
                Debug.Log("[DI] ✓ Camera");
            }

            Debug.Log("[DI] ========== CONFIGURE COMPLETE ==========");
        }

        private void RegisterIfExists<T>(IContainerBuilder builder, System.Action<RegistrationBuilder> configure = null) where T : Component
        {
            var component = FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
            
            if (component != null)
            {
                var registration = builder.RegisterComponent(component);
                configure?.Invoke(registration);
                Debug.Log($"[DI] ✓ {typeof(T).Name}");
            }
            else
            {
                Debug.LogError($"[DI] ✗ {typeof(T).Name} NOT FOUND!");
            }
        }
    }
}


