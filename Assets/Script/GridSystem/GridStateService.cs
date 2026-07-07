using UnityEngine;
using System.Collections.Generic;
using Script.Core.Events;
using VContainer;
using Script.BuildingSystem; // Added to resolve Building type

namespace Script.GridSystem
{
	/// <summary>
	/// Service responsible for managing grid occupancy state and related events.
	/// </summary>
	public class GridStateService : IGridStateService
	{
		private readonly GridOccupancyTracker _occupancyTracker = new();

		#region Public Occupancy Methods (per ResourceManager, ZoneManager)
		public void OccupyCell(Vector2Int cell, GameObject context)
		{
			_occupancyTracker.OccupyCell(cell, context);
		}

		public void FreeCell(Vector2Int cell)
		{
			_occupancyTracker.FreeCell(cell);
		}

		public bool IsCellFree(Vector2Int cell)
		{
			return _occupancyTracker.IsCellFree(cell);
		}

		public IReadOnlyDictionary<Vector2Int, GameObject> GetOccupancySnapshot()
		{
			return _occupancyTracker.GetSnapshot();
		}

		public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
		{
			_occupancyTracker.OccupyCells(originCell, width, height, building);
		}

		public void FreeCells(Vector3Int originCell, int width, int height)
		{
			_occupancyTracker.FreeCells(originCell, width, height);
		}
		#endregion
	}
}
