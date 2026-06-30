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
using Script.UnitSystem;
using Script.UnitSystem.Production;
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
                if (debugMode) Debug.Log("[GameLifetimeScope] ✓ Inizializzato");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameLifetimeScope] ERRORE durante l'inizializzazione: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Regista le classi VContainer dipendenti in ordine di lifecycle.
        /// </summary>
        protected override void Configure(IContainerBuilder builder)
        {
            if (debugMode) Debug.Log("[GameLifetimeScope] Configurazione dei servizi in corso...");

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
            RegisterIfExistsOptional<UnitSelectionCountUI>(builder);

            // UNIT SYSTEM (optional during M1 rollout)
            RegisterIfExistsOptional<UnitRegistryService>(builder);
            RegisterIfExistsOptional<UnitSelectionService>(builder);
            RegisterIfExistsOptional<UnitCommandService>(builder);
            RegisterIfExistsOptional<UnitSelectionInputHandler>(builder);
            RegisterIfExistsOptional<UnitUnlockService>(builder);
            RegisterIfExistsOptional<UnitSpawnService>(builder);
            RegisterIfExistsOptional<UnitProductionQueueService>(builder);
            RegisterIfExistsOptional<UnitRewardGrantService>(builder);

            // OPTIMIZATION SYSTEM
            RegisterIfExists<GridCullingManager>(builder);

            // CAMERA
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                builder.RegisterInstance(mainCamera);
                if (debugMode) Debug.Log("[GameLifetimeScope] ✓ Camera registrata");
            }
            else
            {
                Debug.LogWarning("[GameLifetimeScope] Camera.main non trovata!");
            }

            if (debugMode) Debug.Log("[GameLifetimeScope] ✓ Configurazione completata");
        }

        /// <summary>
        /// Estrae GameObject presenti nella scena e li lega formalmente al contesto Di Container (DependencyInjection).
        /// </summary>
        private void RegisterIfExists<T>(IContainerBuilder builder, Action<RegistrationBuilder> configure = null) where T : Component
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

        /// <summary>
        /// Variante opzionale: non emette errori se il componente non e' ancora presente in scena.
        /// </summary>
        private void RegisterIfExistsOptional<T>(IContainerBuilder builder, Action<RegistrationBuilder> configure = null) where T : Component
        {
            var component = FindFirstObjectByType<T>(FindObjectsInactive.Exclude);

            if (component != null)
            {
                var registration = builder.RegisterComponent(component);
                configure?.Invoke(registration);
                if (debugMode) Debug.Log($"[GameLifetimeScope] ✓ {typeof(T).Name} (optional)");
            }
            else
            {
                if (debugMode) Debug.Log($"[GameLifetimeScope] - {typeof(T).Name} (optional, not found)");
            }
        }
    }
}
