
using System.Collections.Generic;
using Script.BuildingSystem;
using UnityEngine;

namespace Tests.EditMode.BuildingSystem.TestDoubles
{
	/// <summary>
	/// Stub di IGridService per test. Tiene traccia delle celle occupate in un HashSet.
	/// Condiviso tra i test del BuildingSystem per evitare duplicazione.
	/// </summary>
	public class GridServiceStub : IGridService
	{
		private readonly HashSet<Vector2Int> _occupiedCells = new();

		public int Width { get; set; } = 100;
		public int Height { get; set; } = 100;

		public bool AreCellsFree(Vector3Int originCell, int width, int height)
		{
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					var cell = new Vector2Int(originCell.x + x, originCell.y + y);
					if (_occupiedCells.Contains(cell))
						return false;
				}
			}
			return true;
		}

		public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
		{
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					_occupiedCells.Add(new Vector2Int(originCell.x + x, originCell.y + y));
				}
			}
		}

		public void FreeCells(Vector3Int originCell, int width, int height)
		{
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					_occupiedCells.Remove(new Vector2Int(originCell.x + x, originCell.y + y));
				}
			}
		}

		public Vector3 CellToWorld(Vector3Int cell)
		{
			return new Vector3(cell.x, cell.y, 0);
		}

		public bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell)
		{
			cell = new Vector3Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y), 0);
			return true;
		}

		public void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid) { }

		public bool IsCellWalkable(Vector2Int cell)
		{
			return !_occupiedCells.Contains(cell);
		}

		public void GetWalkableNeighbors(Vector2Int cell, List<Vector2Int> neighbors)
		{
			neighbors?.Clear();
		}

		// Helper methods for tests
		public void OccupyCell(int x, int y)
		{
			_occupiedCells.Add(new Vector2Int(x, y));
		}

		public bool WasCellOccupied(int x, int y)
		{
			return _occupiedCells.Contains(new Vector2Int(x, y));
		}
	}
}
