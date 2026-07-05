using UnityEngine;
using System.Collections.Generic;
using Script.BuildingSystem;
using Script.Core.Events;
using VContainer;

namespace Script.GridSystem
{
	/// <summary>
	/// Gestisce la griglia di gioco, integrando TileManager e ZoneManager.
	/// Implementa IGridService per fornire operazioni sulla griglia al BuildingSystem.
	/// </summary>
	public class GridManager : MonoBehaviour, IGridService
	{
		#region Dependencies (Injected by VContainer)

		private TileManager _tileManager;
		private ZoneManager _zoneManager;

		[Inject]
		public void Construct(
			TileManager tileManager,
			ZoneManager zoneManager)
		{
			try
			{
				_tileManager = tileManager;
				_zoneManager = zoneManager;
			}
			catch (System.Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[GridManager] Errore durante Construct: {ex.Message}");
#endif
			}
		}

		#endregion

		#region Private Fields

		private readonly GridOccupancyTracker _occupancyTracker = new();
		private GridPreviewTracker _previewTracker;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			if (!ValidateDependencies())
			{
				return;
			}

			_previewTracker = new GridPreviewTracker(_tileManager);
		}

		private void Start()
		{
			InitializeGrid();
		}

		private void OnDestroy()
		{
			_previewTracker?.ClearPreview();
			_occupancyTracker.Clear();
		}

		#endregion

		#region Initialization

		private bool ValidateDependencies()
		{
			if (_tileManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[GridManager] TileManager non assegnato nell'Inspector! Assegna il riferimento per evitare errori di runtime.");
#endif
				return false;
			}

			if (_zoneManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[GridManager] ZoneManager non assegnato nell'Inspector! Assegna il riferimento per evitare errori di runtime.");
#endif
				return false;
			}

			return true;
		}

		private void InitializeGrid()
		{
			if (_tileManager == null) return;

			// Inizializza la griglia tramite TileManager
			_tileManager.CreateGrid();
			var grid = _tileManager.GetGrid();

			if (_zoneManager == null) return;

			_zoneManager.Initialize(grid, this);
			_zoneManager.CreateZones(_tileManager.Width, _tileManager.Height);

			// Notifica che la griglia è pronta (SaveManager triggera Load all'avvio)
			GlobalEventBus.Publish(new GridInitializedEvent(_tileManager.Width, _tileManager.Height));
		}

		#endregion

		#region Layout Properties

		public int Width => _tileManager.Width;
		public int Height => _tileManager.Height;

		#endregion

		#region Core Grid Queries (IGridService)

		/// <summary>
		/// Converte una posizione nel mondo fisico in una coordinata cella sulla griglia.
		/// </summary>
		public bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell)
		{
			var grid = _tileManager.GetGrid();
			grid.GetWorldToIsoPosition(worldPos, out int x, out int y);
			cell = new Vector3Int(x, y, 0);

			bool inside = x >= 0 && y >= 0 && x < _tileManager.Width && y < _tileManager.Height;

			return inside;
		}

		/// <summary>
		/// Ottiene il centro fisico (World Position) corrispondente a una determinata cella logica.
		/// </summary>
		public Vector3 CellToWorld(Vector3Int cell)
		{
			var grid = _tileManager.GetGrid();
			return grid.GetIsoToWorldPosition(cell.x, cell.y);
		}

		/// <summary>
		/// Verifica se un blocco rettangolare di celle e' completamente libero (state Unlocked e non occupato).
		/// </summary>
		public bool AreCellsFree(Vector3Int originCell, int width, int height)
		{
			for (int dx = 0; dx < width; dx++)
			{
				for (int dy = 0; dy < height; dy++)
				{
					int x = originCell.x + dx;
					int y = originCell.y + dy;

					// Bounds check
					if (x < 0 || y < 0 || x >= _tileManager.Width || y >= _tileManager.Height)
						return false;

					var p = new Vector2Int(x, y);

					// Check occupancy (unica fonte di verità)
					if (!_occupancyTracker.IsCellFree(p))
						return false;

					// Check tile state
					var tile = _tileManager.GetGrid().GetValue(x, y);
					if (tile == null || tile.State != TileState.Unlocked)
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Segna come occupate le celle specificate, associandole a un edificio e pubblicando CellsOccupiedEvent.
		/// </summary>
		public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
		{
			_occupancyTracker.OccupyCells(originCell, width, height, building);
		}

		/// <summary>
		/// Libera un blocco di celle occupate e pubblica CellsFreedEvent.
		/// </summary>
		public void FreeCells(Vector3Int originCell, int width, int height)
		{
			_occupancyTracker.FreeCells(originCell, width, height);
		}

		/// <summary>
		/// Applica una visuale di preview per un blocco di celle (es. verde per valido, rosso per invalido).
		/// </summary>
		public void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid)
		{
			_previewTracker?.SetCellsPreview(originCell, width, height, isValid);
		}

		#endregion

		#region Complex Mapping

		/// <summary>
		/// Helper: trova la cella il cui centro logico è più vicino a worldPos, valutando i 9 vicini.
		/// </summary>
		private bool TryWorldToNearestCell(Vector3 worldPos, out Vector3Int bestCell)
		{
			bestCell = default;
			// Conversione grezza
			if (!TryWorldToCell(worldPos, out var approx)) return false;

			float bestDist = float.MaxValue;
			var grid = _tileManager.GetGrid();

			// Esamina le 9 celle attorno a approx (inclusa)
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -1; dy <= 1; dy++)
				{
					int cx = approx.x + dx;
					int cy = approx.y + dy;
					if (cx < 0 || cy < 0 || cx >= _tileManager.Width || cy >= _tileManager.Height) continue;

					var candidate = new Vector3Int(cx, cy, 0);

					// Centro VISIVO del tile (non il pivot): usa SpriteRenderer.bounds.center se disponibile
					Vector3 center;
					var tile = grid.GetValue(cx, cy);
					if (tile != null)
					{
						var sr = tile.GetComponent<SpriteRenderer>();
						center = sr != null ? sr.bounds.center : tile.transform.position;
					}
					else
					{
						// Fallback: centro matematico della cella
						center = CellToWorld(candidate);
					}

					// Distanza 2D su piano di gioco (x,y)
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

		/// <summary>
		/// Esegue un raycast dalla telecamera per convertire un punto sullo schermo nella cella d'intersezione sul piano orizzontale.
		/// </summary>
		public bool TryScreenToCell(Camera cam, Vector3 screenPos, out Vector3Int cell)
		{
			cell = default;
			if (cam == null) return false;

			// Ray dal mouse
			var ray = cam.ScreenPointToRay(screenPos);

			if (Mathf.Approximately(ray.direction.z, 0f)) return false;
			float t = -ray.origin.z / ray.direction.z;
			if (t < 0f) return false; // Dietro la camera

			Vector3 worldOnPlane = ray.origin + ray.direction * t; // Punto world proiettato sul piano di gioco

			// Usa snapping alla cella con centro più vicino per correggere bordi del diamante
			return TryWorldToNearestCell(worldOnPlane, out cell);
		}

		#endregion

		#region Pathfinding Support

		/// <summary>
		/// Verifica se una cella è attraversabile da unità (non bloccata da ostacoli o zone inesplorate).
		/// </summary>
		public bool IsCellWalkable(Vector2Int cell)
		{
			// Bounds check
			if (cell.x < 0 || cell.y < 0 || cell.x >= _tileManager.Width || cell.y >= _tileManager.Height)
				return false;

			// Tile must exist
			var tile = _tileManager.GetGrid().GetValue(cell.x, cell.y);
			if (tile == null)
				return false;

			// Check occupancy (buildings + resources + signs already in _occupiedCells)
			if (!_occupancyTracker.IsCellFree(cell))
				return false;

			return true;
		}

		/// <summary>
		/// Esegue query sulla griglia per estrarre tutti i vicini attraversabili (8-way).
		/// </summary>
		public void GetWalkableNeighbors(Vector2Int cell, List<Vector2Int> neighbors)
		{
			if (neighbors == null) return;

			neighbors.Clear();

			// OPTIMIZED: 8-directional movement (4 cardinal + 4 diagonal)
			// Reuse caller-owned buffer to avoid per-call allocations.
			Vector2Int east = new Vector2Int(cell.x + 1, cell.y);
			if (IsValidCell(east) && IsCellWalkable(east))
				neighbors.Add(east);

			Vector2Int west = new Vector2Int(cell.x - 1, cell.y);
			if (IsValidCell(west) && IsCellWalkable(west))
				neighbors.Add(west);

			Vector2Int north = new Vector2Int(cell.x, cell.y + 1);
			if (IsValidCell(north) && IsCellWalkable(north))
				neighbors.Add(north);

			Vector2Int south = new Vector2Int(cell.x, cell.y - 1);
			if (IsValidCell(south) && IsCellWalkable(south))
				neighbors.Add(south);

			// ===== DIAGONAL DIRECTIONS (cost = sqrt(2) ≈ 1.414) =====
			// Only allow diagonal if BOTH adjacent cardinals are walkable (prevent corner-cutting)
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
		/// Verifica se una cella è all'interno dei limiti della griglia.
		/// Fast bounds check before IsCellWalkable (avoids out-of-bounds access).
		/// </summary>
		private bool IsValidCell(Vector2Int cell)
		{
			return cell.x >= 0 && cell.x < _tileManager.Width &&
				   cell.y >= 0 && cell.y < _tileManager.Height;
		}
		#endregion

		#region Public Occupancy Methods (per ResourceManager, ZoneManager)

		/// <summary>
		/// Occupa una singola cella (risorse, sign, etc).
		/// </summary>
		public void OccupyCell(Vector2Int cell, GameObject context)
		{
			_occupancyTracker.OccupyCell(cell, context);
		}

		/// <summary>
		/// Libera una singola cella.
		/// </summary>
		public void FreeCell(Vector2Int cell)
		{
			_occupancyTracker.FreeCell(cell);
		}

		/// <summary>
		/// Verifica se una singola cella è libera.
		/// </summary>
		public bool IsCellFree(Vector2Int cell)
		{
			return _occupancyTracker.IsCellFree(cell);
		}

		/// <summary>
		/// Snapshot di tutte le occupanze (utility per save game, debugging).
		/// </summary>
		public IReadOnlyDictionary<Vector2Int, GameObject> GetOccupancySnapshot()
		{
			return _occupancyTracker.GetSnapshot();
		}

		#endregion
	}
}
