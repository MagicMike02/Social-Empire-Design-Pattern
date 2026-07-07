using UnityEngine;
using VContainer;
using Script.Core.Events;

namespace Script.GridSystem
{
	/// <summary>
	/// Service responsible for bootstrapping the grid at application start.
	/// It creates the tile grid, initializes zones and publishes <see cref="GridInitializedEvent"/>.
	/// </summary>
	public class GridBootstrapService
	{
		private TileManager _tileManager;
		private ZoneManager _zoneManager;

		[Inject]
		public void Construct(TileManager tileManager, ZoneManager zoneManager)
		{
			_tileManager = tileManager;
			_zoneManager = zoneManager;
		}

		/// <summary>
		/// Creates the grid, initializes zones and notifies the system that the grid is ready.
		/// </summary>
		/// <summary>
		/// Creates the grid, initializes zones and notifies the system that the grid is ready.
		/// The <paramref name="gridManager"/> is supplied by the caller (GridManager) to avoid a circular DI dependency.
		/// </summary>
		public void InitializeGrid(GridManager gridManager)
		{
			if (_tileManager == null)
			{
				Debug.LogError("[GridBootstrapService] TileManager is null – cannot initialize grid.");
				return;
			}

			// Create the underlying tile grid.
			_tileManager.CreateGrid();
			var grid = _tileManager.GetGrid();

			if (_zoneManager != null)
			{
				// Pass the GridManager so that ZoneManager can occupy cells for purchase signs.
				_zoneManager.Initialize(grid, gridManager);
				_zoneManager.CreateZones(_tileManager.Width, _tileManager.Height);
			}
			else
			{
				Debug.LogWarning("[GridBootstrapService] ZoneManager is null – zones will not be created.");
			}

			// Publish the event so other systems (SaveManager, Pathfinding, etc.) can react.
			GlobalEventBus.Publish(new GridInitializedEvent(_tileManager.Width, _tileManager.Height));
		}
	}
}