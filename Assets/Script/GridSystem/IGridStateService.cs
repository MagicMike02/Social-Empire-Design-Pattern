using System.Collections.Generic;
using Script.BuildingSystem;
using UnityEngine;

namespace Script.GridSystem
{
	/// <summary>
	/// Public contract for the grid state service. Exposes occupancy‑related operations
	/// required by query services while keeping the concrete implementation decoupled.
	/// </summary>
	public interface IGridStateService
	{
		bool IsCellFree(Vector2Int cell);
		void OccupyCell(Vector2Int cell, GameObject context);
		void FreeCell(Vector2Int cell);
		IReadOnlyDictionary<Vector2Int, GameObject> GetOccupancySnapshot();
		void OccupyCells(Vector3Int originCell, int width, int height, Building building);
		void FreeCells(Vector3Int originCell, int width, int height);
	}
}