using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Script.BuildingSystem;
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
    public sealed class SaveManager : ISaveable
    {
        #region Dependencies

        private readonly IPersistenceManager _persistence;
        private readonly GameEconomyManager _economyManager;
        private readonly GridManager _gridManager;
        private readonly ZoneManager _zoneManager;

        [Inject]
        public SaveManager(
            IPersistenceManager persistence,
            GameEconomyManager economyManager,
            GridManager gridManager,
            ZoneManager zoneManager)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _economyManager = economyManager ?? throw new ArgumentNullException(nameof(economyManager));
            _gridManager = gridManager ?? throw new ArgumentNullException(nameof(gridManager));
            _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
        }

        #endregion

        #region Constants

        private const int CurrentSchemaVersion = 1;

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
                playerLevel = 1, // TODO: collegare a PlayerProgressionManager quando disponibile
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

            // TODO: istanziare i prefab tramite BuildingFactory quando integrato.
            // Per ora logga le posizioni da ripristinare.
            foreach (var kvp in data.placedBuildings)
            {
                var parts = kvp.Key.Split(',');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                    continue;

#if UNITY_EDITOR
                Debug.Log($"[SaveManager] Restore building '{kvp.Value}' at ({x},{y}) — TODO: BuildingFactory integration.");
#endif
            }
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
