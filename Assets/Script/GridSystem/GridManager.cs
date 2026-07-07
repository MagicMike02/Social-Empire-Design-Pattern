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
			ZoneManager zoneManager,
			IGridStateService stateService,
			GridQueryService queryService,
			GridVisualService visualService)
		{
			try
			{
				_tileManager = tileManager;
				_zoneManager = zoneManager;
				_stateService = stateService;
				_queryService = queryService;
				_visualService = visualService;
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
		private IGridStateService _stateService;
		private GridQueryService _queryService;
		private GridVisualService _visualService;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			if (!ValidateDependencies())
			{
				return;
			}
		}

		private void Start()
		{
			InitializeGrid();
		}

		private void OnDestroy()
		{
			_visualService?.ClearPreview();
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
			if (_queryService != null)
			{
				return _queryService.TryWorldToCell(worldPos, out cell);
			}
			// Ensure out parameter is assigned even when service is missing.
			cell = default;
			return false;
		}

		/// <summary>
		/// Ottiene il centro fisico (World Position) corrispondente a una determinata cella logica.
		/// </summary>
		public Vector3 CellToWorld(Vector3Int cell)
		{
			return _queryService?.CellToWorld(cell) ?? Vector3.zero;
		}

		/// <summary>
		/// Segna come occupate le celle specificate, associandole a un edificio e pubblicando CellsOccupiedEvent.
		/// </summary>
		public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
		{
			_stateService?.OccupyCells(originCell, width, height, building);
		}

		/// <summary>
		/// Libera un blocco di celle occupate e pubblica CellsFreedEvent.
		/// </summary>
		public void FreeCells(Vector3Int originCell, int width, int height)
		{
			_stateService?.FreeCells(originCell, width, height);
		}

		/// <summary>
		/// Imposta la preview visuale su un'area di celle.
		/// Delega al GridVisualService.
		/// </summary>
		public void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid)
		{
			_visualService?.SetCellsPreview(originCell, width, height, isValid);
		}

		/// <summary>
		/// Verifica se un blocco rettangolare di celle è completamente libero (state Unlocked e non occupato).
		/// </summary>
		public bool AreCellsFree(Vector3Int originCell, int width, int height)
		{
			return _queryService?.AreCellsFree(originCell, width, height) ?? false;
		}

		/// <summary>
		/// Raycasts from the camera to map a screen point onto the horizontal grid plane.
		/// Delegates the actual conversion to <see cref="GridQueryService"/>.
		/// </summary>
		public bool TryScreenToCell(Camera cam, Vector3 screenPos, out Vector3Int cell)
		{
			if (_queryService != null)
			{
				return _queryService.TryScreenToCell(cam, screenPos, out cell);
			}
			// Assign default to satisfy out parameter requirement.
			cell = default;
			return false;
		}

		#endregion

		#region Pathfinding Support

		/// <summary>
		/// Verifica se una cella è attraversabile da unità (non bloccata da ostacoli o zone inesplorate).
		/// </summary>
		public bool IsCellWalkable(Vector2Int cell)
		{
			return _queryService?.IsCellWalkable(cell) ?? false;
		}

		/// <summary>
		/// Esegue query sulla griglia per estrarre tutti i vicini attraversabili (8-way).
		/// </summary>
		public void GetWalkableNeighbors(Vector2Int cell, List<Vector2Int> neighbors)
		{
			_queryService?.GetWalkableNeighbors(cell, neighbors);
		}
		#endregion

		#region Public Occupancy Methods (per ResourceManager, ZoneManager)

		/// <summary>
		/// Occupa una singola cella (risorse, sign, etc).
		/// </summary>
		public void OccupyCell(Vector2Int cell, GameObject context)
		{
			_stateService?.OccupyCell(cell, context);
		}

		/// <summary>
		/// Libera una singola cella.
		/// </summary>
		public void FreeCell(Vector2Int cell)
		{
			_stateService?.FreeCell(cell);
		}

		/// <summary>
		/// Verifica se una singola cella è libera.
		/// </summary>
		public bool IsCellFree(Vector2Int cell)
		{
			return _stateService?.IsCellFree(cell) ?? false;
		}

		/// <summary>
		/// Snapshot di tutte le occupanze (utility per save game, debugging).
		/// </summary>
		public IReadOnlyDictionary<Vector2Int, GameObject> GetOccupancySnapshot()
		{
			return _stateService?.GetOccupancySnapshot();
		}

		#endregion
	}
}
