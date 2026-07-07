using UnityEngine;
using System.Collections.Generic;
using VContainer;

namespace Script.GridSystem
{
	/// <summary>
	/// Service providing query operations on the grid (coordinate conversion, walkability, neighbor lookup, etc.).
	/// </summary>
	public class GridQueryService
	{
		private TileManager _tileManager;
		// Reference to the state service for occupancy queries (replaces reflection).
		private IGridStateService _stateService;
		// Cache dimensions to avoid repeated property look‑ups.
		private int _width;
		private int _height;

		[Inject]
		public void Construct(TileManager tileManager, IGridStateService stateService)
		{
			_tileManager = tileManager;
			// Directly store the injected state service; no reflection needed.
			_stateService = stateService;
			// Cache grid dimensions for fast bounds checks.
			_width = _tileManager.Width;
			_height = _tileManager.Height;
		}

		public bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell)
		{
			var grid = _tileManager.GetGrid();
			grid.GetWorldToIsoPosition(worldPos, out int x, out int y);
			cell = new Vector3Int(x, y, 0);
			bool inside = x >= 0 && y >= 0 && x < _width && y < _height;
			return inside;
		}

		public Vector3 CellToWorld(Vector3Int cell)
		{
			var grid = _tileManager.GetGrid();
			return grid.GetIsoToWorldPosition(cell.x, cell.y);
		}

		public bool AreCellsFree(Vector3Int originCell, int width, int height)
		{
			for (int dx = 0; dx < width; dx++)
			{
				for (int dy = 0; dy < height; dy++)
				{
					int x = originCell.x + dx;
					int y = originCell.y + dy;
					if (x < 0 || y < 0 || x >= _width || y >= _height)
						return false;
					var p = new Vector2Int(x, y);
					if (!_stateService.IsCellFree(p))
						return false;
					var tile = _tileManager.GetGrid().GetValue(x, y);
					if (tile == null || tile.State != TileState.Unlocked)
						return false;
				}
			}
			return true;
		}

		public bool IsCellWalkable(Vector2Int cell)
		{
			if (cell.x < 0 || cell.y < 0 || cell.x >= _width || cell.y >= _height)
				return false;
			var tile = _tileManager.GetGrid().GetValue(cell.x, cell.y);
			if (tile == null) return false;
			if (!_stateService.IsCellFree(cell)) return false;
			return true;
		}

		/// <summary>
		/// Populates <paramref name="neighbors"/> with orthogonal and diagonal walkable neighbours.
		/// Diagonals are added only when both adjacent orthogonal cells are walkable (prevents corner‑cutting).
		/// </summary>
		/// <param name="cell">The centre cell for which neighbours are queried.</param>
		/// <param name="neighbors">A list that will receive the resulting neighbour cells.</param>
		public void GetWalkableNeighbors(Vector2Int cell, List<Vector2Int> neighbors)
		{
			if (neighbors == null) return;
			neighbors.Clear();

			// ---- Orthogonal neighbours (N, S, E, W) ----
			Vector2Int east = new Vector2Int(cell.x + 1, cell.y);
			if (IsValidCell(east) && IsCellWalkable(east)) neighbors.Add(east);
			Vector2Int west = new Vector2Int(cell.x - 1, cell.y);
			if (IsValidCell(west) && IsCellWalkable(west)) neighbors.Add(west);
			Vector2Int north = new Vector2Int(cell.x, cell.y + 1);
			if (IsValidCell(north) && IsCellWalkable(north)) neighbors.Add(north);
			Vector2Int south = new Vector2Int(cell.x, cell.y - 1);
			if (IsValidCell(south) && IsCellWalkable(south)) neighbors.Add(south);

			// ---- Diagonal neighbours (corner‑cutting guard) ----
			// Only add a diagonal if both adjacent orthogonal cells are also walkable.
			Vector2Int northEast = new Vector2Int(cell.x + 1, cell.y + 1);
			if (IsValidCell(northEast) && IsCellWalkable(northEast) && IsCellWalkable(east) && IsCellWalkable(north))
				neighbors.Add(northEast);
			Vector2Int northWest = new Vector2Int(cell.x - 1, cell.y + 1);
			if (IsValidCell(northWest) && IsCellWalkable(northWest) && IsCellWalkable(west) && IsCellWalkable(north))
				neighbors.Add(northWest);
			Vector2Int southEast = new Vector2Int(cell.x + 1, cell.y - 1);
			if (IsValidCell(southEast) && IsCellWalkable(southEast) && IsCellWalkable(east) && IsCellWalkable(south))
				neighbors.Add(southEast);
			Vector2Int southWest = new Vector2Int(cell.x - 1, cell.y - 1);
			if (IsValidCell(southWest) && IsCellWalkable(southWest) && IsCellWalkable(west) && IsCellWalkable(south))
				neighbors.Add(southWest);
		}

		/// <summary>
		/// Determines whether a cell lies within the cached grid bounds.
		/// </summary>
		private bool IsValidCell(Vector2Int cell)
		{
			return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _height;
		}

		public bool TryScreenToCell(Camera cam, Vector3 screenPos, out Vector3Int cell)
		{
			cell = default;
			if (cam == null) return false;
			var ray = cam.ScreenPointToRay(screenPos);
			if (Mathf.Approximately(ray.direction.z, 0f)) return false;
			float t = -ray.origin.z / ray.direction.z;
			if (t < 0f) return false;
			Vector3 worldOnPlane = ray.origin + ray.direction * t;
			return TryWorldToNearestCell(worldOnPlane, out cell);
		}

		private bool TryWorldToNearestCell(Vector3 worldPos, out Vector3Int bestCell)
		{
			// Initialise output and obtain an approximate cell via the fast conversion.
			bestCell = default;
			if (!TryWorldToCell(worldPos, out var approx)) return false;

			float bestDist = float.MaxValue;
			var grid = _tileManager.GetGrid();

			// Search the 3×3 neighbourhood around the approximate cell.
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -1; dy <= 1; dy++)
				{
					int cx = approx.x + dx;
					int cy = approx.y + dy;
					// Skip out‑of‑bounds candidates using cached dimensions.
					if (cx < 0 || cy < 0 || cx >= _width || cy >= _height) continue;
					var candidate = new Vector3Int(cx, cy, 0);
					Vector3 center;
					var tile = grid.GetValue(cx, cy);
					if (tile != null)
					{
						// Use the sprite renderer bounds if available; otherwise fall back to the transform position.
						var sr = tile.GetComponent<SpriteRenderer>();
						center = sr != null ? sr.bounds.center : tile.transform.position;
					}
					else
					{
						// No tile – compute centre from cell coordinates.
						center = CellToWorld(candidate);
					}
					float d2 = (new Vector2(center.x, center.y) - new Vector2(worldPos.x, worldPos.y)).sqrMagnitude;
					if (d2 < bestDist)
					{
						bestDist = d2;
						bestCell = candidate;
					}
				}
			}
			return bestDist < float.MaxValue;
		}
	}
}
