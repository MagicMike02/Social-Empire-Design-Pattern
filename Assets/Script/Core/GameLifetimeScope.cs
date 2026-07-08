using System;
using Script.BuildingSystem;
using Script.Common;
using Script.Core.AutoSave;
using Script.Core.Commands;
using Script.Core.SaveSystem;
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
		[SerializeField] private AutoSaveConfigSO _autoSaveConfig;

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

			// Register GridManager both as its interface and as concrete type for injection into other services.
			RegisterIfExists<GridManager>(builder, r =>
			{
				r.As<IGridService>();
				r.AsSelf();
			});

			builder.Register<GridStateService>(Lifetime.Singleton).As<IGridStateService>();
			// GridQueryService and GridVisualService are lightweight; use Transient to avoid unintended shared state.
			builder.Register<GridQueryService>(Lifetime.Transient);
			builder.Register<GridVisualService>(Lifetime.Transient);
			// Register the new bootstrap service as a Singleton.
			builder.Register<GridBootstrapService>(Lifetime.Singleton);

			// RESOURCE SYSTEM
			RegisterIfExists<ResourceSpawner>(builder);
			RegisterIfExists<ResourcePoolManager>(builder);
			RegisterIfExists<ResourceManager>(builder);
#if UNITY_EDITOR
			RegisterIfExists<ResourceEditorTools>(builder);
#endif

			// BUILDING SYSTEM
			builder.Register<BuildingCatalog>(Lifetime.Singleton).As<IBuildingCatalog>();
			RegisterIfExists<BuildingFactory>(builder);
			RegisterIfExists<BuildingManager>(builder);

			RegisterIfExists<PrefabPoolManager>(builder);
			RegisterIfExists<GenericPreviewSystem>(builder);
			builder.Register<BuildingValidationService>(Lifetime.Singleton);
			RegisterIfExists<BuildingPlacer>(builder);
			RegisterIfExists<PlacementInputHandler>(builder);

			// INPUT SYSTEM
			RegisterIfExists<InputManager>(builder);

			// PATHFINDING SYSTEM
			RegisterIfExists<PathfindingManager>(builder);

#if UNITY_EDITOR
			// TEST UTILITIES (solo in Editor)
			// RegisterIfExists<PathfindingTester>(builder);
#endif

			// UI SYSTEM
			RegisterIfExists<UIManager>(builder);
			RegisterIfExists<ResourceDisplayUI>(builder);

			// OPTIMIZATION SYSTEM
			RegisterIfExists<GridCullingManager>(builder);

			// SAVE SYSTEM
			builder.Register<JsonSaveSystem>(Lifetime.Singleton).As<IPersistenceManager>();
			builder.Register<SaveManager>(Lifetime.Singleton);

			// AUTOSAVE — lifecycle gestito da AutoSaveLifecycle (IStartable + IDisposable)
			builder.Register<AutoSaveManager>(Lifetime.Singleton)
				   .As<IAutoSaveManager>();

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

		#region App Lifecycle - Save Hooks

		/// <summary>
		/// Persiste il timestamp di uscita quando l'app viene messa in pausa
		/// (es. utente passa ad un'altra app, preme Home, riceve una chiamata).
		/// </summary>
		private void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus)
			{
				SaveOnExit();
			}
		}

		/// <summary>
		/// Persiste il timestamp di uscita quando l'app viene chiusa o terminata.
		/// </summary>
		private void OnApplicationQuit()
		{
			SaveOnExit();
		}

		/// <summary>
		/// Risolve SaveManager dal container e invoca Save() per persistere lastExitAt.
		/// SaveManager.Save() imposta già lastExitAt = DateTime.UtcNow.ToString("o").
		/// </summary>
		private void SaveOnExit()
		{
			try
			{
				if (Container == null)
				{
#if UNITY_EDITOR
					Debug.LogWarning("[GameLifetimeScope] Container null: salvataggio di uscita saltato.");
#endif
					return;
				}

				if (!Container.TryResolve<SaveManager>(out var saveManager) || saveManager == null)
				{
#if UNITY_EDITOR
					Debug.LogWarning("[GameLifetimeScope] SaveManager non risolvibile: salvataggio di uscita saltato.");
#endif
					return;
				}

				saveManager.Save();
#if UNITY_EDITOR
				if (debugMode)
				{
					Debug.Log("[GameLifetimeScope] ✓ Salvataggio di uscita completato (lastExitAt persistito).");
				}
#endif
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[GameLifetimeScope] Errore durante il salvataggio di uscita: {ex.Message}");
#endif
			}
		}

		#endregion

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
