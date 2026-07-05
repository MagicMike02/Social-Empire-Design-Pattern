using System.Collections.Generic;

namespace Script.Core.DTO
{
	/// <summary>
	/// Snapshot immutabile dello stato della griglia ai fini di validazione.
	/// Record C# puro — nessuna dipendenza UnityEngine.
	/// </summary>
	/// <remarks>
	/// Modella SOLO le informazioni necessarie alla validazione:
	/// - Dimensioni della griglia (bounds)
	/// - Edifici piazzati (occupancy) mappati per coordinata (x,y) → buildingId
	/// - Celle sbloccate (tile state Unlocked) come set di coordinate
	/// </remarks>
	public record GridStateData
	{
		public int Width { get; init; }
		public int Height { get; init; }

		/// <summary>Edifici piazzati: chiave (x,y), valore buildingId.</summary>
		public IReadOnlyDictionary<(int, int), string> PlacedBuildings { get; init; }

		/// <summary>Coordinate delle celle sbloccate (TileState.Unlocked).</summary>
		/// <remarks>
		/// Usa <see cref="HashSet{T}"/> anziché <c>IReadOnlySet</c> perché
		/// <c>IReadOnlySet</c> è disponibile solo da .NET 5+; il progetto
		/// target è netstandard2.1. L'immutabilità è garantita a livello
		/// di API dal modificatore <c>init</c>.
		/// </remarks>
		public HashSet<(int, int)> UnlockedCells { get; init; }

		public GridStateData(
			int width,
			int height,
			IReadOnlyDictionary<(int, int), string> placedBuildings = null,
			HashSet<(int, int)> unlockedCells = null)
		{
			Width = width;
			Height = height;
			PlacedBuildings = placedBuildings ?? new Dictionary<(int, int), string>();
			UnlockedCells = unlockedCells ?? new HashSet<(int, int)>();
		}

		/// <summary>Griglia vuota di dimensioni date (nessun edificio, tutte celle bloccate).</summary>
		public static GridStateData Empty(int width, int height) =>
			new(width, height);

		/// <summary>Griglia completamente libera e sbloccata (utile per test).</summary>
		public static GridStateData FullyUnlocked(int width, int height)
		{
			var unlocked = new HashSet<(int, int)>();
			for (int x = 0; x < width; x++)
				for (int y = 0; y < height; y++)
					unlocked.Add((x, y));
			return new GridStateData(width, height, null, unlocked);
		}
	}
}
