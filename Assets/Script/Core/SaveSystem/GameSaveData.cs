
using System;
using System.Collections.Generic;
using Script.ResourceSystem.Enums;

namespace Script.Core.SaveSystem
{
	/// <summary>
	/// DTO per l'intero stato di gioco.
	/// Progettato per serializzazione JSON sparsa: solo tile occupate, non l'intera griglia 100x100.
	/// Serializzato con Newtonsoft.Json (supporta Dictionary nativi, null, polimorfismo).
	/// NON usa [Serializable] Unity: serializzazione solo via Newtonsoft.Json.
	/// </summary>
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
		/// Serializzato come dizionario { "Wood": 100, "Stone": 50, ... }.
		/// </summary>
		public Dictionary<string, int> resources;

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
		public Dictionary<string, string> placedBuildings;

		#endregion

		#region Zones

		/// <summary>
		/// Indici delle zone sbloccate (es. [0, 1, 2, 5, 6]).
		/// </summary>
		// List of unlocked zones expressed as top‑left cell coordinates.
		// Using a simple struct makes the data explicit and works with Newtonsoft.Json.
		public List<ZoneCoord> unlockedZones;

		/// <summary>
		/// Dimensione di una zona in tile (es. 20 per zone 20x20).
		/// </summary>
		public int zoneSize;

		/// <summary>
		/// Simple coordinate container for a zone's top‑left cell.
		/// Newtonsoft.Json can serialize plain structs without Unity-specific attributes.
		/// </summary>
		public struct ZoneCoord
		{
			public int x;
			public int y;

			public ZoneCoord(int x, int y)
			{
				this.x = x;
				this.y = y;
			}
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
				resources = new Dictionary<string, int>(),
				playerLevel = 1, // TODO(progression): raccordo con PlayerProgressionManager (default nuova partita)
				gridWidth = gridWidth,
				gridHeight = gridHeight,
				placedBuildings = new Dictionary<string, string>(),
				// Inizializza con la zona di partenza (top‑left cell 0,0).
				unlockedZones = new List<ZoneCoord> { new ZoneCoord(0, 0) },
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
			if (resources == null || placedBuildings == null || unlockedZones == null) return false;

			// Verifica che gli edifici siano dentro i bounds della griglia
			foreach (var kvp in placedBuildings)
			{
				var parts = kvp.Key.Split(',');
				if (parts.Length != 2) return false;
				if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
					return false;
				if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight)
					return false;
				if (string.IsNullOrEmpty(kvp.Value))
					return false;
			}

			return true;
		}

		#endregion
	}
}