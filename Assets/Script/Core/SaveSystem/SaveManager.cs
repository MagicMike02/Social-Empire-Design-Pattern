using System;
using System.Collections.Generic;
using UnityEngine;
using Script.BuildingSystem;
using Script.Core.Events;
using Script.EconomySystem;
using Script.GridSystem;
using Script.ResourceSystem.Enums;
using VContainer;

namespace Script.Core.SaveSystem
{
	/// <summary>
	/// Coordinatore centrale della persistenza.
	/// Implementa ISaveable: raccoglie lo stato da tutti i manager, costruisce il GameSaveData
	/// e delega la serializzazione a IPersistenceManager (JsonSaveSystem).
	/// Al caricamento, distribuisce lo stato ai manager.
	/// </summary>
	public sealed class SaveManager : ISaveable, IDisposable
	{
		#region Dependencies

		private readonly IPersistenceManager _persistence;
		private readonly GameEconomyManager _economyManager;
		private readonly GridManager _gridManager;
		private readonly ZoneManager _zoneManager;
		private readonly BuildingManager _buildingManager;

		[Inject]
		public SaveManager(
			IPersistenceManager persistence,
			GameEconomyManager economyManager,
			GridManager gridManager,
			ZoneManager zoneManager,
			BuildingManager buildingManager)
		{
			_persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
			_economyManager = economyManager ?? throw new ArgumentNullException(nameof(economyManager));
			_gridManager = gridManager ?? throw new ArgumentNullException(nameof(gridManager));
			_zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
			_buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));

			// Sottoscrive GridInitializedEvent per triggerare Load() dopo che la griglia è pronta.
			GlobalEventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
		}

		#endregion

		#region Constants

		private const int CurrentSchemaVersion = 1;

		// TODO(progression): sostituire con PlayerProgressionManager.CurrentLevel quando disponibile.
		private const int CurrentDefaultPlayerLevel = 1;

		#endregion

		#region IDisposable

		/// <summary>
		/// Disiscrive da GlobalEventBus per prevenire memory leak.
		/// Chiamato da GameLifetimeScope.OnDestroy.
		/// </summary>
		public void Dispose()
		{
			GlobalEventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Triggerato quando GridManager completa InitializeGrid.
		/// Esegue Load() per ripristinare lo stato salvato all'avvio del gioco.
		/// </summary>
		private void OnGridInitialized(GridInitializedEvent _)
		{
			Load();
		}

		#endregion

		#region ISaveable Implementation

		/// <summary>
		/// Raccoglie lo stato corrente da tutti i manager e lo serializza su disco.
		/// </summary>
		public GameSaveData Save()
		{
			var data = new GameSaveData
			{
				schemaVersion = CurrentSchemaVersion,
				savedAt = DateTime.UtcNow.ToString("o"),
				lastExitAt = DateTime.UtcNow.ToString("o"),
				resources = GatherResources(),
				playerLevel = CurrentDefaultPlayerLevel, // TODO(progression): raccordo con PlayerProgressionManager
				gridWidth = _gridManager.Width,
				gridHeight = _gridManager.Height,
				placedBuildings = GatherPlacedBuildings(),
				unlockedZoneIndices = GatherUnlockedZoneIndices(),
				zoneSize = _zoneManager.ZoneSize
			};

			if (!data.IsValid())
			{
#if UNITY_EDITOR
				Debug.LogError("[SaveManager] GameSaveData non valido prima del salvataggio.");
#endif
				return null;
			}

			try
			{
				_persistence.SaveGame(data);
#if UNITY_EDITOR
				Debug.Log("[SaveManager] Salvataggio completato.");
#endif
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[SaveManager] Errore durante il salvataggio su disco: {ex.Message}");
#endif
				return null;
			}

			return data;
		}

		/// <summary>
		/// Carica il GameSaveData da disco e distribuisce lo stato ai manager.
		/// </summary>
		public void Load(GameSaveData data)
		{
			if (data == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning("[SaveManager] Load chiamato con data null. Nessun caricamento eseguito.");
#endif
				return;
			}

			if (!data.IsValid())
			{
#if UNITY_EDITOR
				Debug.LogError("[SaveManager] GameSaveData caricato non valido. Caricamento annullato.");
#endif
				return;
			}

			// TODO: migrazioni schema future (data.schemaVersion < CurrentSchemaVersion)
			RestoreResources(data);
			RestorePlacedBuildings(data);
			RestoreZones(data);
#if UNITY_EDITOR
			Debug.Log("[SaveManager] Caricamento completato.");
#endif
		}

		/// <summary>
		/// Carica il salvataggio da disco tramite IPersistenceManager e distribuisce lo stato.
		/// Overload senza parametri per uso UI (S3-08).
		/// Restituisce il GameSaveData caricato, o null se non disponibile.
		/// </summary>
		public GameSaveData Load()
		{
			try
			{
				var data = _persistence.LoadGame();
				if (data == null)
				{
#if UNITY_EDITOR
					Debug.LogWarning("[SaveManager] Nessun salvataggio trovato su disco.");
#endif
					return null;
				}

				Load(data);
				return data;
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[SaveManager] Errore durante il caricamento da disco: {ex.Message}");
#endif
				return null;
			}
		}

		#endregion

		#region Gather (Save)

		private Dictionary<string, int> GatherResources()
		{
			var snapshot = _economyManager.GetResourcesSnapshot();
			var result = new Dictionary<string, int>(snapshot.Count);

			foreach (var kvp in snapshot)
			{
				result[kvp.Key.ToString()] = kvp.Value;
			}

			return result;
		}

		private Dictionary<string, string> GatherPlacedBuildings()
		{
			var occupancy = _gridManager.GetOccupancySnapshot();
			var result = new Dictionary<string, string>(occupancy.Count);

			foreach (var kvp in occupancy)
			{
				Vector2Int cell = kvp.Key;
				GameObject go = kvp.Value;
				if (go == null) continue;

				var building = go.GetComponent<Building>();
				if (building == null || building.Config == null) continue;

				string key = $"{cell.x},{cell.y}";
				string buildingId = building.Config.name;
				result[key] = buildingId;
			}

			return result;
		}

		private List<int> GatherUnlockedZoneIndices()
		{
			var coords = _zoneManager.GetUnlockedZoneCoords();
			var result = new List<int>(coords.Count);

			int zoneSize = _zoneManager.ZoneSize;
			if (zoneSize <= 0) return result;

			int gridWidth = _gridManager.Width;
			foreach (var coord in coords)
			{
				// Indice lineare: zoneCoord.x + zoneCoord.y * (gridWidth / zoneSize)
				int zonesPerRow = gridWidth / zoneSize;
				int linearIndex = coord.x + coord.y * zonesPerRow;
				result.Add(linearIndex);
			}

			return result;
		}

		#endregion

		#region Restore (Load)

		private void RestoreResources(GameSaveData data)
		{
			if (data.resources == null) return;

			foreach (var kvp in data.resources)
			{
				if (Enum.TryParse<ResourceType>(kvp.Key, out ResourceType type))
				{
					_economyManager.SetResource(type, kvp.Value);
				}
#if UNITY_EDITOR
				else
				{
					Debug.LogWarning($"[SaveManager] ResourceType non riconosciuto: {kvp.Key}");
				}
#endif
			}
		}

		private void RestorePlacedBuildings(GameSaveData data)
		{
			if (data.placedBuildings == null) return;
			if (_buildingManager?.Factory == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning("[SaveManager] BuildingFactory non disponibile: restore edifici saltato.");
#endif
				return;
			}

			var catalog = LoadBuildingCatalog();
			var grid = _gridManager;

			foreach (var kvp in data.placedBuildings)
			{
				var parts = kvp.Key.Split(',');
				if (parts.Length != 2) continue;
				if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
					continue;

				if (!catalog.TryGetValue(kvp.Value, out var config) || config == null)
				{
#if UNITY_EDITOR
					Debug.LogWarning($"[SaveManager] BuildingConfigSO '{kvp.Value}' non trovato in Resources/Buildings. Edificio ({x},{y}) saltato.");
#endif
					continue;
				}

				var originCell = new Vector3Int(x, y, 0);
				if (!grid.AreCellsFree(originCell, config.Width, config.Height))
				{
#if UNITY_EDITOR
					Debug.LogWarning($"[SaveManager] Celle ({x},{y}) non libere al restore. Edificio '{kvp.Value}' saltato.");
#endif
					continue;
				}

				Vector3 worldPos = grid.CellToWorld(originCell);
				var building = _buildingManager.Factory.CreateBuilding(config, worldPos, _buildingManager.Root);
				if (building == null) continue;

				grid.OccupyCells(originCell, config.Width, config.Height, building);
			}
		}

		/// <summary>
		/// Carica tutti i BuildingConfigSO da Resources/Buildings e li indicizza per nome asset.
		/// Usato per mappare il buildingId salvato (Config.name) al runtime config durante il restore.
		/// TODO: sostituire con un IBuildingCatalog dedicato quando introdotto.
		/// </summary>
		private Dictionary<string, BuildingConfigSO> LoadBuildingCatalog()
		{
			var configs = Resources.LoadAll<BuildingConfigSO>("Buildings");
			var catalog = new Dictionary<string, BuildingConfigSO>(configs.Length);
			foreach (var c in configs)
			{
				if (c != null) catalog[c.name] = c;
			}
			return catalog;
		}

		private void RestoreZones(GameSaveData data)
		{
			if (data.unlockedZoneIndices == null) return;

			int zoneSize = _zoneManager.ZoneSize;
			int gridWidth = data.gridWidth;
			if (zoneSize <= 0 || gridWidth <= 0) return;

			int zonesPerRow = gridWidth / zoneSize;

			foreach (int linearIndex in data.unlockedZoneIndices)
			{
				int zx = linearIndex % zonesPerRow;
				int zy = linearIndex / zonesPerRow;
				var coord = new Vector2Int(zx, zy);

				_zoneManager.SetZoneUnlockedState(coord, true);
			}
		}

		#endregion
	}
}
