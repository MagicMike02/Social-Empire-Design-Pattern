using UnityEngine;

namespace Script.Core.Events
{
	/// <summary>
	/// Pubblicato quando una o più celle della griglia vengono occupate.
	/// Publisher: GridManager
	/// Subscribers: Pathfinding (invalida cache), Minimap, FogOfWar
	/// </summary>
	public readonly struct CellsOccupiedEvent
	{
		public readonly Vector3Int OriginCell;
		public readonly int Width;
		public readonly int Height;
		public readonly GameObject Occupant;

		public CellsOccupiedEvent(Vector3Int originCell, int width, int height, GameObject occupant)
		{
			OriginCell = originCell;
			Width = width;
			Height = height;
			Occupant = occupant;
		}
	}

	/// <summary>
	/// Pubblicato quando celle della griglia vengono liberate.
	/// Publisher: GridManager
	/// Subscribers: Pathfinding (invalida cache), BuildingPlacer (aggiorna validità preview)
	/// </summary>
	public readonly struct CellsFreedEvent
	{
		public readonly Vector3Int OriginCell;
		public readonly int Width;
		public readonly int Height;

		public CellsFreedEvent(Vector3Int originCell, int width, int height)
		{
			OriginCell = originCell;
			Width = width;
			Height = height;
		}
	}

	/// <summary>
	/// Pubblicato quando una zona viene sbloccata (acquistata dal giocatore).
	/// Publisher: ZoneManager
	/// Subscribers: UI (aggiorna mappa), Audio, Tutorial
	/// </summary>
	public readonly struct ZoneUnlockedEvent
	{
		public readonly int ZoneIndex;
		public readonly Vector2Int ZonePosition;

		public ZoneUnlockedEvent(int zoneIndex, Vector2Int zonePosition)
		{
			ZoneIndex = zoneIndex;
			ZonePosition = zonePosition;
		}
	}

	/// <summary>
	/// Pubblicato quando il tentativo di acquisto zona fallisce.
	/// Publisher: ZoneManager
	/// Subscribers: ZoneFeedbackUI (mostra errore), AudioManager (suono errore)
	/// </summary>
	public readonly struct ZonePurchaseFailedEvent
	{
		public readonly Vector2Int ZoneCoord;
		public readonly string Reason;

		public ZonePurchaseFailedEvent(Vector2Int zoneCoord, string reason)
		{
			ZoneCoord = zoneCoord;
			Reason = reason;
		}
	}

	/// <summary>
	/// Pubblicato quando la griglia è stata completamente inizializzata.
	/// Publisher: GridManager (fine InitializeGrid)
	/// Subscribers: SaveManager (trigger Load all'avvio), Pathfinding (init navmesh)
	/// </summary>
	public readonly struct GridInitializedEvent
	{
		public readonly int GridWidth;
		public readonly int GridHeight;

		public GridInitializedEvent(int gridWidth, int gridHeight)
		{
			GridWidth = gridWidth;
			GridHeight = gridHeight;
		}
	}
}
