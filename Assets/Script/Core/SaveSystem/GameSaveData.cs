
using System;
using Script.ResourceSystem.Enums;

namespace Script.Core.SaveSystem
{
    /// <summary>
    /// DTO serializzabile per l'intero stato di gioco.
    /// Progettato per serializzazione JSON sparsa: solo tile occupate, non l'intera griglia 100x100.
    /// Compatibile con JsonUtility (Unity) — usa tipi serializzabili, non record C# puri.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        #region Metadata

        /// <summary>
        /// Versione dello schema per migrazioni future del formato save.
        /// </summary>
        public int schemaVersion;

        /// <summary>
        /// Timestamp UTC dell'ultimo salvataggio.
        /// </summary>
        public string savedAt;

        /// <summary>
        /// Timestamp UTC dell'ultima uscita dal gioco (OnApplicationQuit / OnApplicationPause).
        /// Usato da OfflineProgressCalculator per calcolare delta risorse al ritorno.
        /// </summary>
        public string lastExitAt;

        #endregion

        #region Economy

        /// <summary>
        /// Stato dell'economia: risorse possedute dal giocatore.
        /// Serializzato come array di coppie chiave-valore (JsonUtility non supporta Dictionary nativamente).
        /// </summary>
        public ResourceEntry[] resources;

        /// <summary>
        /// Livello del giocatore (progressione).
        /// </summary>
        public int playerLevel;

        #endregion

        #region Grid (formato sparse)

        /// <summary>
        /// Larghezza della griglia di gioco.
        /// </summary>
        public int gridWidth;

        /// <summary>
        /// Altezza della griglia di gioco.
        /// </summary>
        public int gridHeight;

        /// <summary>
        /// Solo celle occupate da edifici — formato sparse {(x,y): buildingId}.
        /// Non serializza le 10.000 tile vuote.
        /// </summary>
        public PlacedBuildingEntry[] placedBuildings;

        #endregion

        #region Zones

        /// <summary>
        /// Indici delle zone sbloccate (es. [0, 1, 2, 5, 6]).
        /// </summary>
        public int[] unlockedZoneIndices;

        /// <summary>
        /// Dimensione di una zona in tile (es. 20 per zone 20x20).
        /// </summary>
        public int zoneSize;

        #endregion

        #region Nested Serializable Types

        /// <summary>
        /// Coppia chiave-valore per risorsa (JsonUtility-compatibile).
        /// </summary>
        [Serializable]
        public struct ResourceEntry
        {
            public string type;
            public int amount;

            public ResourceEntry(ResourceType type, int amount)
            {
                this.type = type.ToString();
                this.amount = amount;
            }
        }

        /// <summary>
        /// Entry per un edificio piazzato sulla griglia (formato sparse).
        /// </summary>
        [Serializable]
        public struct PlacedBuildingEntry
        {
            /// <summary>Coordinata X sulla griglia (origine, angolo in basso a sinistra dell'edificio).</summary>
            public int x;

            /// <summary>Coordinata Y sulla griglia (origine).</summary>
            public int y;

            /// <summary>Identificatore del tipo di edificio (es. "house_1", "farm").</summary>
            public string buildingId;

            /// <summary>Timestamp UTC di quando l'edificio è stato piazzato.</summary>
            public string placedAt;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Crea un GameSaveData vuoto con valori di default per un nuovo gioco.
        /// </summary>
        public static GameSaveData CreateDefault(int gridWidth, int gridHeight, int zoneSize)
        {
            return new GameSaveData
            {
                schemaVersion = 1,
                savedAt = DateTime.UtcNow.ToString("o"),
                lastExitAt = DateTime.UtcNow.ToString("o"),
                resources = System.Array.Empty<ResourceEntry>(),
                playerLevel = 1,
                gridWidth = gridWidth,
                gridHeight = gridHeight,
                placedBuildings = System.Array.Empty<PlacedBuildingEntry>(),
                unlockedZoneIndices = new[] { 0 }, // solo zona iniziale sbloccata
                zoneSize = zoneSize
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Verifica che il save data sia internamente consistente.
        /// </summary>
        public bool IsValid()
        {
            if (schemaVersion < 1) return false;
            if (gridWidth <= 0 || gridHeight <= 0) return false;
            if (zoneSize <= 0) return false;
            if (resources == null || placedBuildings == null || unlockedZoneIndices == null) return false;

            // Verifica che gli edifici siano dentro i bounds della griglia
            foreach (var b in placedBuildings)
            {
                if (b.x < 0 || b.y < 0 || b.x >= gridWidth || b.y >= gridHeight)
                    return false;
                if (string.IsNullOrEmpty(b.buildingId))
                    return false;
            }

            return true;
        }

        #endregion
    }
}