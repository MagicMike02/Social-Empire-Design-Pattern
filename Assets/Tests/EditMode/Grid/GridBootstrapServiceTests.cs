using NUnit.Framework;
using Script.GridSystem;
using UnityEngine;

namespace Tests.EditMode.Grid
{
	/// <summary>
	/// Very lightweight test that ensures <see cref="GridBootstrapService.InitializeGrid"/> can be called
	/// without throwing when a valid <see cref="TileManager"/> is supplied and the optional
	/// <see cref="ZoneManager"/> is null.
	/// </summary>
	public class GridBootstrapServiceTests
	{
		private TileManager _tileManager;
		private GridBootstrapService _bootstrapService;

		[SetUp]
		public void SetUp()
		{
			var go = new GameObject("BootstrapTest");
			_tileManager = go.AddComponent<TileManager>();

			// Initialise the serialized fields via reflection (same technique as other test).
			var type = typeof(TileManager);
			type.GetField("width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SetValue(_tileManager, 8);
			type.GetField("height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SetValue(_tileManager, 8);
			type.GetField("cellSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SetValue(_tileManager, 1f);

			_tileManager.CreateGrid();

			_bootstrapService = new GridBootstrapService();
			// Construct with TileManager and a null ZoneManager (allowed by implementation).
			_bootstrapService.Construct(_tileManager, null);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_tileManager.gameObject);
		}

		[Test]
		public void InitializeGrid_WithValidTileManager_DoesNotThrow()
		{
			// GridManager is not required for the current implementation – we can pass null.
			Assert.DoesNotThrow(() => _bootstrapService.InitializeGrid(null));
		}
	}
}
