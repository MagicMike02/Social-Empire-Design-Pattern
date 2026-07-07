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
using Tests.EditMode.BuildingSystem.TestDoubles;
using UnityEngine.TestTools;

namespace Tests.EditMode.BuildingSystem
{
	public class DestroyBuildingCommandTests
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
		private Building _existingBuilding;
		private GameObject _existingBuildingGO;
		private Vector3Int _gridPosition;

		[SetUp]
		public void SetUp()
		{
			_gridService = new GridServiceStub();
			_gridPosition = new Vector3Int(5, 5, 0);

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
			_config.name = "TestHouse";
			_config.Width = 1;
			_config.Height = 1;
			_config.Costs = new List<BuildingConfigSO.ResourceCost>
			{
				new() { Type = ResourceType.Wood, Amount = 10 },
				new() { Type = ResourceType.Gold, Amount = 5 }
			};

			// Setup Building prefab (used by Factory for pool)
			_buildingPrefab = new GameObject("BuildingPrefab");
			_buildingPrefab.AddComponent<Building>();
			_config.Prefab = _buildingPrefab;

			// Create the existing building via Factory (simulates a placed building)
			Vector3 worldPos = new Vector3(_gridPosition.x, _gridPosition.y, 0);
			_existingBuilding = _factory.CreateBuilding(_config, worldPos, _buildingManager.Root);
			_existingBuildingGO = _existingBuilding.gameObject;

			// Occupy cells on the grid as if the building was already placed
			_gridService.OccupyCells(_gridPosition, _config.Width, _config.Height, _existingBuilding);
		}

		[TearDown]
		public void TearDown()
		{
			// Cleanup in reverse order
			if (_existingBuildingGO != null)
				Object.DestroyImmediate(_existingBuildingGO);
			if (_buildingPrefab != null)
				Object.DestroyImmediate(_buildingPrefab);
			if (_buildingManagerGO != null)
				Object.DestroyImmediate(_buildingManagerGO);
			if (_factoryGO != null)
				Object.DestroyImmediate(_factoryGO);
			if (_poolManagerGO != null)
				Object.DestroyImmediate(_poolManagerGO);
			if (_economyGO != null)
				Object.DestroyImmediate(_economyGO);
			if (_config != null)
				ScriptableObject.DestroyImmediate(_config);
		}

		#region Execute Tests

		[Test]
		public void Execute_WhenBuildingExists_FreesCellsAndRefundsAndDestroys()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Pre-assert: cells are occupied, economy has no resources
			Assert.That(_gridService.WasCellOccupied(_gridPosition.x, _gridPosition.y), Is.True);
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(0));
			Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(0));

			// Act
			bool result = command.Execute();

			// Assert
			Assert.That(result, Is.True);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));

			// Cells should be freed
			Assert.That(_gridService.WasCellOccupied(_gridPosition.x, _gridPosition.y), Is.False);

			// Resources should be refunded at 50%
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(5));  // 10 * 0.5
			Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(2));  // 5 * 0.5 = 2 (FloorToInt)

			// Building GameObject should be destroyed
			Assert.That(_existingBuildingGO == null, Is.True);
		}

		[Test]
		public void Execute_WhenCalledTwice_SecondCallReturnsFalse()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Act - First call
			bool firstResult = command.Execute();
			Assert.That(firstResult, Is.True);

			// Act - Second call (building already destroyed)
			bool secondResult = command.Execute();

			// Assert
			Assert.That(secondResult, Is.False);
		}

		#endregion

		#region Undo Tests

		[Test]
		public void Undo_AfterExecute_RebuildsBuildingAndOccupiesCells()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Execute first
			command.Execute();

			// Pre-assert: cells are free after destroy
			Assert.That(_gridService.WasCellOccupied(_gridPosition.x, _gridPosition.y), Is.False);

			// Act
			bool result = command.Undo();

			// Assert
			Assert.That(result, Is.True);
			Assert.That(command.State, Is.EqualTo(CommandState.RolledBack));

			// Cells should be occupied again
			Assert.That(_gridService.WasCellOccupied(_gridPosition.x, _gridPosition.y), Is.True);

			// Resources should be subtracted back (refund reversed)
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(0));
			Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(0));
		}

		[Test]
		public void Undo_WhenFactoryFails_ReturnsFalse()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Execute first
			command.Execute();

			// Break the config's prefab so Factory returns null
			_config.Prefab = null;

			// Expect error log from Factory (CreateBuilding returns null)
			//LogAssert.Expect(LogType.Error, new Regex("Impossibile creare edificio"));

			// Act
			bool result = command.Undo();

			// Assertmj
			Assert.That(result, Is.False);
		}

		[Test]
		public void Undo_WithoutExecute_ReturnsFalse()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Act (no Execute before Undo)
			bool result = command.Undo();

			// Assert
			Assert.That(result, Is.False);
		}

		#endregion

		#region ExecuteAsync Tests

		[Test]
		public async Task ExecuteAsync_WhenSuccessful_ReturnsTrueAndConfirms()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Act
			bool result = await command.ExecuteAsync();

			// Assert
			Assert.That(result, Is.True);
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));

			// Cells should be freed
			Assert.That(_gridService.WasCellOccupied(_gridPosition.x, _gridPosition.y), Is.False);

			// Resources should be refunded at 50%
			Assert.That(_economy.GetResourceAmount(ResourceType.Wood), Is.EqualTo(5));
			Assert.That(_economy.GetResourceAmount(ResourceType.Gold), Is.EqualTo(2));
		}

		[Test]
		public async Task ExecuteAsync_WhenExecuteFails_ReturnsFalse()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Use reflection to set _originalBuilding to null to force Execute to fail
			var field = typeof(DestroyBuildingCommand)
				.GetField("_originalBuilding", BindingFlags.NonPublic | BindingFlags.Instance);
			field?.SetValue(command, null);

			// Act
			bool result = await command.ExecuteAsync();

			// Assert
			Assert.That(result, Is.False);
		}

		#endregion

		#region State Tests

		[Test]
		public void State_Initially_IsPending()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Act & Assert
			Assert.That(command.State, Is.EqualTo(CommandState.Pending));
		}

		[Test]
		public void Description_ReturnsFormattedString()
		{
			// Arrange
			var command = new DestroyBuildingCommand(
				_buildingManager, _gridService, _economy, _existingBuilding, _gridPosition);

			// Act & Assert
			Assert.That(command.Description, Is.EqualTo("Destroy TestHouse at (5, 5, 0)"));
		}

		#endregion

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
	}
}