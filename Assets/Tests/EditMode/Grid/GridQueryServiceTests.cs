using System.Collections.Generic;
using Script.BuildingSystem;
using NUnit.Framework;
using Script.GridSystem;
using UnityEngine;

namespace Tests.EditMode.Grid
{
	/// <summary>
	/// Simple unit‑tests for <see cref="GridQueryService"/> covering the most common public methods.
	/// Uses a real <see cref="TileManager"/> component instantiated on a temporary GameObject and a lightweight mock
	/// implementation of <see cref="IGridStateService"/>.
	/// </summary>
	public class GridQueryServiceTests
	{
		private TileManager _tileManager;
		private MockGridStateService _stateService;
		private GridQueryService _queryService;

		[SetUp]
		public void SetUp()
		{
			// Create a GameObject and attach TileManager (MonoBehaviour).
			var go = new GameObject("TileManagerTest");
			_tileManager = go.AddComponent<TileManager>();

			// Initialise private serialized fields via reflection (width, height, cellSize).
			var type = typeof(TileManager);
			type.GetField("width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SetValue(_tileManager, 10);
			type.GetField("height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SetValue(_tileManager, 10);
			type.GetField("cellSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SetValue(_tileManager, 1f);

			// Create the logical grid (no prefab needed for these tests).
			_tileManager.CreateGrid();

			// Simple mock that reports every cell as free.
			_stateService = new MockGridStateService();

			_queryService = new GridQueryService();
			_queryService.Construct(_tileManager, _stateService);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_tileManager.gameObject);
		}

		[Test]
		public void TryWorldToCell_RoundTrip_ReturnsOriginalCell()
		{
			// Choose a known cell and obtain its world position via the underlying grid.
			var grid = _tileManager.GetGrid();
			var worldPos = grid.GetIsoToWorldPosition(3, 4);

			var success = _queryService.TryWorldToCell(worldPos, out var cell);
			Assert.IsTrue(success);
			Assert.AreEqual(3, cell.x);
			Assert.AreEqual(4, cell.y);
		}

		[Test]
		public void CellToWorld_CachesResult_ReturnsSameInstance()
		{
			var cell = new Vector3Int(2, 2, 0);
			var first = _queryService.CellToWorld(cell);
			var second = _queryService.CellToWorld(cell);
			// The values should be identical; caching ensures no recomputation.
			Assert.AreEqual(first, second);
		}

		[Test]
		public void AreCellsFree_AllFree_ReturnsTrue()
		{
			var origin = new Vector3Int(1, 1, 0);
			var result = _queryService.AreCellsFree(origin, 3, 2);
			Assert.IsTrue(result);
		}

		[Test]
		public void IsCellWalkable_ValidAndFree_ReturnsTrue()
		{
			var cell = new Vector2Int(5, 5);
			var result = _queryService.IsCellWalkable(cell);
			Assert.IsTrue(result);
		}

		// ---------------------------------------------------------------------
		// Mock implementation of IGridStateService used only for these tests.
		// ---------------------------------------------------------------------
		private class MockGridStateService : IGridStateService
		{
			private readonly HashSet<Vector2Int> _occupied = new();

			public bool IsCellFree(Vector2Int cell) => !_occupied.Contains(cell);

			public void OccupyCell(Vector2Int cell, GameObject context) => _occupied.Add(cell);

			public void FreeCell(Vector2Int cell) => _occupied.Remove(cell);

			public IReadOnlyDictionary<Vector2Int, GameObject> GetOccupancySnapshot() => new Dictionary<Vector2Int, GameObject>();

			public void OccupyCells(Vector3Int originCell, int width, int height, Building building) { }

			public void FreeCells(Vector3Int originCell, int width, int height) { }
		}
	}
}
