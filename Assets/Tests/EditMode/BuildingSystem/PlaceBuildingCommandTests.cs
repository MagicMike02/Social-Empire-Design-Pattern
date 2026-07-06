using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Script.BuildingSystem;
using Script.BuildingSystem.Commands;
using Script.Common;
using Script.Core.Commands;
using Script.EconomySystem;
using Script.ResourceSystem.Enums;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.EditMode.BuildingSystem
{
	public class PlaceBuildingCommandTests
	{
		private GameEconomyManager _economy;
		private GameObject _economyGO;
		private BuildingManager _buildingManager;
		private GameObject _buildingManagerGO;
		private BuildingFactory _factory;
		private GameObject _factoryGO;
		private PrefabPoolManager _poolManager;
		private GameObject _poolManagerGO;
		private GridServiceStub _gridService;
		private BuildingConfigSO _config;
		private GameObject _buildingPrefab;

		[SetUp]
		public void SetUp()
		{
			_gridService = new GridServiceStub();

			// Setup Economy
			_economyGO = new GameObject("Economy");
			_economy = _economyGO.AddComponent<GameEconomyManager>();
			InvokePrivateAwake(_economy);

			// Setup PrefabPoolManager
			_poolManagerGO = new GameObject("PoolManager");
			_poolManager = _poolManagerGO.AddComponent<PrefabPoolManager>();
			InvokePrivateAwake(_poolManager);

			// Setup BuildingFactory
			_factoryGO = new GameObject("Factory");
			_factory = _factoryGO.AddComponent<BuildingFactory>();
			_factory.Construct(_poolManager);

			// Setup BuildingManager
			_buildingManagerGO = new GameObject("BuildingManager");
			_buildingManager = _buildingManagerGO.AddComponent<BuildingManager>();
			_buildingManager.Construct(_economy, _gridService, _factory);
			InvokePrivateAwake(_buildingManager);

			// Setup BuildingConfigSO
			_config = ScriptableObject.CreateInstance<BuildingConfigSO>();
			_config.Width = 1;
			_config.Height = 1;
			_config.Costs = new List<BuildingConfigSO.ResourceCost>
			{
				new() { Type = ResourceType.Wood, Amount = 10 },
				new() { Type = ResourceType.Gold, Amount = 5 }
			};

			// Setup Building prefab
			_buildingPrefab = new GameObject("BuildingPrefab");
			_buildingPrefab.AddComponent<Building>();
			_config.Prefab = _buildingPrefab;
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_buildingPrefab);
			Object.DestroyImmediate(_config);
			Object.DestroyImmediate(_buildingManagerGO);
			Object.DestroyImmediate(_factoryGO);
			Object.DestroyImmediate(_poolManagerGO);
			Object.DestroyImmediate(_economyGO);
		}

		[Test]
		public void Execute_WhenCellsFreeAndCanAfford_PlacesBuildingAndSpendsResources()
		{
			// Arrange
			_economy.SetResource(ResourceType.Wood, 20);
			_economy.SetResource(ResourceType.Gold, 10);
			var position = new Vector3Int(5, 5, 0);

			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act
			bool result = command.Execute();

			// Assert
			Assert.That(result, Is.True);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(10));
			Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(5));
			Assert.That(_gridService.WasCellOccupied(5, 5), Is.True);
		}

		[Test]
		public void Execute_WhenCellsOccupied_ReturnsFalse()
		{
			// Arrange
			_economy.SetResource(ResourceType.Wood, 20);
			_economy.SetResource(ResourceType.Gold, 10);
			_gridService.OccupyCell(5, 5);
			var position = new Vector3Int(5, 5, 0);

			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act
			bool result = command.Execute();

			// Assert
			Assert.That(result, Is.False);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(20));
			Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(10));
		}

		[Test]
		public void Execute_WhenCannotAfford_ReturnsFalse()
		{
			// Arrange
			_economy.SetResource(ResourceType.Wood, 5);
			_economy.SetResource(ResourceType.Gold, 1);
			var position = new Vector3Int(5, 5, 0);

			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act
			bool result = command.Execute();

			// Assert
			Assert.That(result, Is.False);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
			Assert.That(_gridService.WasCellOccupied(5, 5), Is.False);
		}

		[Test]
		public void Execute_WhenFactoryReturnsNull_ReturnsFalse()
		{
			// Arrange
			_economy.SetResource(ResourceType.Wood, 20);
			_economy.SetResource(ResourceType.Gold, 10);
			_config.Prefab = null; // Factory returns null when prefab is null
			var position = new Vector3Int(5, 5, 0);

			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Expect error log from Factory (CreateBuilding returns null)
			LogAssert.Expect(LogType.Error, new Regex("Impossibile creare edificio"));

			// Act
			bool result = command.Execute();

			// Assert
			Assert.That(result, Is.False);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(20));
		}

		[Test]
		public void Undo_AfterExecute_FreesCellsAndRefundsResources()
		{
			// Arrange
			_economy.SetResource(ResourceType.Wood, 20);
			_economy.SetResource(ResourceType.Gold, 10);
			var position = new Vector3Int(5, 5, 0);

			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);
			command.Execute();

			// Act
			bool result = command.Undo();

			// Assert
			Assert.That(result, Is.True);
			Assert.That(command.State, Is.EqualTo(CommandState.RolledBack));
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(20));
			Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(10));
			Assert.That(_gridService.WasCellOccupied(5, 5), Is.False);
		}

		[Test]
		public void Undo_WhenBuildingNeverPlaced_ReturnsFalse()
		{
			// Arrange
			var position = new Vector3Int(5, 5, 0);
			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act
			bool result = command.Undo();

			// Assert
			Assert.That(result, Is.False);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
		}

		[Test]
		public async Task ExecuteAsync_WhenSuccessful_ReturnsTrueAndConfirms()
		{
			// Arrange
			_economy.SetResource(ResourceType.Wood, 20);
			_economy.SetResource(ResourceType.Gold, 10);
			var position = new Vector3Int(5, 5, 0);

			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act
			bool result = await command.ExecuteAsync();

			// Assert
			Assert.That(result, Is.True);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(10));
			Assert.That(_gridService.WasCellOccupied(5, 5), Is.True);
		}

		[Test]
		public async Task ExecuteAsync_WhenExecuteFails_ReturnsFalse()
		{
			// Arrange
			_economy.SetResource(ResourceType.Wood, 2);
			var position = new Vector3Int(5, 5, 0);

			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act
			bool result = await command.ExecuteAsync();

			// Assert
			Assert.That(result, Is.False);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
		}

		[Test]
		public void State_Initially_IsPending()
		{
			// Arrange
			var position = new Vector3Int(5, 5, 0);
			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act & Assert
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
		}

		[Test]
		public void Description_ReturnsFormattedString()
		{
			// Arrange
			_config.name = "TestHouse";
			var position = new Vector3Int(3, 7, 0);
			var command = new PlaceBuildingCommand(
				_buildingManager, _gridService, _economy, _config, position);

			// Act & Assert
			Assert.That(command.Description, Is.EqualTo("Place TestHouse at (3, 7, 0)"));
		}

		#region Helper Methods

		/// <summary>
		/// Invoca il metodo privato Awake() su un MonoBehaviour tramite reflection.
		/// </summary>
		private static void InvokePrivateAwake(MonoBehaviour behaviour)
		{
			var method = behaviour.GetType()
				.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
			method?.Invoke(behaviour, null);
		}

		#endregion

		#region Test Doubles

		/// <summary>
		/// Stub di IGridService per test. Tiene traccia delle celle occupate in un HashSet.
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

		#endregion
	}
}