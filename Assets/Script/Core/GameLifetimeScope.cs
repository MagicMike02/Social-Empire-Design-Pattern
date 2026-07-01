using System;
using Script.BuildingSystem;
using Script.Common;
using Script.Core.Commands;
using Script.EconomySystem;
using Script.GridSystem;
using Script.InputSystem;
using Script.PathfindingSystem;
using Script.ResourceSystem;
using Script.ResourceSystem.ResourceUI;
using Script.UI;
using Script.Core.Optimization;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Script.Core
{
    /// <summary>
    /// VContainer LifetimeScope 
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
                if (debugMode)
                {
#if UNITY_EDITOR
                    Debug.Log("[GameLifetimeScope] ✓ Inizializzato");
#endif
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[GameLifetimeScope] ERRORE durante l'inizializzazione: {ex.Message}\n{ex.StackTrace}");
#endif
                throw;
            }
        }

        /// <summary>
        /// Regista le classi VContainer dipendenti in ordine di lifecycle.
        /// </summary>
        protected override void Configure(IContainerBuilder builder)
        {
            if (debugMode)
            {
#if UNITY_EDITOR
                Debug.Log("[GameLifetimeScope] Configurazione dei servizi in corso...");
#endif
            }

            // CORE - Economy
            RegisterIfExists<GameEconomyManager>(builder);
            
            // CORE - Command System (Undo/Redo universale)
            builder.Register<CommandHistory>(Lifetime.Singleton);
            RegisterIfExists<CommandInputHandler>(builder);
            
            // GRID SYSTEM
            RegisterIfExists<TileManager>(builder);
            RegisterIfExists<ZoneManager>(builder);
            RegisterIfExists<GridManager>(builder, r => r.As<IGridService>());
            
            // RESOURCE SYSTEM
            RegisterIfExists<ResourceSpawner>(builder);
            RegisterIfExists<ResourcePoolManager>(builder);
            RegisterIfExists<ResourceManager>(builder);
            
            // BUILDING SYSTEM
            RegisterIfExists<BuildingFactory>(builder);
            RegisterIfExists<BuildingManager>(builder);
            
            RegisterIfExists<PrefabPoolManager>(builder);
            RegisterIfExists<GenericPreviewSystem>(builder);
            RegisterIfExists<BuildingPlacer>(builder);
            RegisterIfExists<PlacementInputHandler>(builder);
            
            // INPUT SYSTEM
            RegisterIfExists<InputManager>(builder);
            
            // PATHFINDING SYSTEM
            RegisterIfExists<PathfindingManager>(builder);
            
            #if UNITY_EDITOR
            // TEST UTILITIES (solo in Editor)
            RegisterIfExists<PathfindingTester>(builder);
            #endif
            
            // UI SYSTEM
            RegisterIfExists<UIManager>(builder);
            RegisterIfExists<ResourceDisplayUI>(builder);

            // OPTIMIZATION SYSTEM
            RegisterIfExists<GridCullingManager>(builder);

            // CAMERA
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                builder.RegisterInstance(mainCamera);
                if (debugMode)
                {
#if UNITY_EDITOR
                    Debug.Log("[GameLifetimeScope] ✓ Camera registrata");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("[GameLifetimeScope] Camera.main non trovata!");
#endif
            }

            if (debugMode)
            {
#if UNITY_EDITOR
                Debug.Log("[GameLifetimeScope] ✓ Configurazione completata");
#endif
            }
        }

        /// <summary>
        /// Estrae GameObject presenti nella scena e li lega formalmente al contesto Di Container (DependencyInjection).
        /// </summary>
        private void RegisterIfExists<T>(IContainerBuilder builder, Action<RegistrationBuilder> configure = null) where T : Component
        {
            var component = FindAnyObjectByType<T>(FindObjectsInactive.Exclude);
            
            if (component != null)
            {
                var registration = builder.RegisterComponent(component);
                configure?.Invoke(registration);
                if (debugMode)
                {
#if UNITY_EDITOR
                    Debug.Log($"[GameLifetimeScope] ✓ {typeof(T).Name}");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError($"[GameLifetimeScope] ✗ {typeof(T).Name} non trovato in scena!");
#endif
            }
        }
    }
}
