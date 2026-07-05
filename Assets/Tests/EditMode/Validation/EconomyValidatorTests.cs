using System;
using System.Collections.Generic;
using NUnit.Framework;
using Script.Core.DTO;
using Script.EconomySystem.Validation;

namespace SocialEmpire.Tests.EditMode.Validation
{
	[TestFixture]
	public class EconomyValidatorTests
	{
		#region Test Data Builders

		private static PlayerEconomyData Economy(params (string, int)[] resources)
		{
			var dict = new Dictionary<string, int>();
			foreach (var (type, amount) in resources)
				dict[type] = amount;
			return new PlayerEconomyData(dict);
		}

		private static IReadOnlyDictionary<string, int> Costs(params (string, int)[] costs)
		{
			var dict = new Dictionary<string, int>();
			foreach (var (type, amount) in costs)
				dict[type] = amount;
			return dict;
		}

		private static BuildingConfigData Building(double cooldownSeconds = 0) =>
			new("test", 1, 1, null, 0, cooldownSeconds);

		#endregion

		#region CanAfford — multi-resource

		[Test]
		public void CanAfford_MultiResource_AllCovered_ReturnsOk()
		{
			var economy = Economy(("Gold", 100), ("Wood", 50));
			var costs = Costs(("Gold", 30), ("Wood", 20));

			var result = new EconomyValidator().CanAfford(economy, costs);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_MultiResource_ExactAmount_ReturnsOk()
		{
			var economy = Economy(("Gold", 50));
			var costs = Costs(("Gold", 50));

			var result = new EconomyValidator().CanAfford(economy, costs);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_MultiResource_InsufficientOne_ReturnsFailWithDetail()
		{
			var economy = Economy(("Gold", 10), ("Wood", 50));
			var costs = Costs(("Gold", 30), ("Wood", 20));

			var result = new EconomyValidator().CanAfford(economy, costs);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("Gold"));
			Assert.IsTrue(result.Reason.Contains("30"));
			Assert.IsTrue(result.Reason.Contains("10"));
		}

		[Test]
		public void CanAfford_MultiResource_MissingResource_ReturnsFail()
		{
			var economy = Economy(("Gold", 100));
			var costs = Costs(("Gold", 30), ("Wood", 20));

			var result = new EconomyValidator().CanAfford(economy, costs);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("Wood"));
			Assert.IsTrue(result.Reason.Contains("ha 0"));
		}

		[Test]
		public void CanAfford_MultiResource_MultipleMissing_ReportsAll()
		{
			var economy = Economy(("Gold", 5));
			var costs = Costs(("Gold", 30), ("Wood", 20), ("Stone", 10));

			var result = new EconomyValidator().CanAfford(economy, costs);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("Gold"));
			Assert.IsTrue(result.Reason.Contains("Wood"));
			Assert.IsTrue(result.Reason.Contains("Stone"));
		}

		[Test]
		public void CanAfford_MultiResource_NullCosts_ReturnsOk()
		{
			var economy = Economy(("Gold", 0));

			var result = new EconomyValidator().CanAfford(economy, null);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_MultiResource_EmptyCosts_ReturnsOk()
		{
			var economy = Economy(("Gold", 0));
			var costs = Costs();

			var result = new EconomyValidator().CanAfford(economy, costs);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_MultiResource_ZeroOrNegativeCosts_Skipped()
		{
			var economy = Economy(("Gold", 0));
			var costs = Costs(("Gold", 0), ("Wood", -5));

			var result = new EconomyValidator().CanAfford(economy, costs);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_MultiResource_NullEconomy_ReturnsFail()
		{
			var costs = Costs(("Gold", 10));

			var result = new EconomyValidator().CanAfford(null, costs);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("null"));
		}

		#endregion

		#region CanAfford — single resource

		[Test]
		public void CanAfford_Single_Sufficient_ReturnsOk()
		{
			var economy = Economy(("Gold", 100));

			var result = new EconomyValidator().CanAfford(economy, "Gold", 50);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_Single_ExactAmount_ReturnsOk()
		{
			var economy = Economy(("Gold", 50));

			var result = new EconomyValidator().CanAfford(economy, "Gold", 50);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_Single_Insufficient_ReturnsFailWithDetail()
		{
			var economy = Economy(("Gold", 10));

			var result = new EconomyValidator().CanAfford(economy, "Gold", 50);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("Gold"));
			Assert.IsTrue(result.Reason.Contains("50"));
			Assert.IsTrue(result.Reason.Contains("10"));
		}

		[Test]
		public void CanAfford_Single_MissingResource_ReturnsFail()
		{
			var economy = Economy(("Wood", 100));

			var result = new EconomyValidator().CanAfford(economy, "Gold", 50);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("ha 0"));
		}

		[Test]
		public void CanAfford_Single_ZeroAmount_ReturnsOk()
		{
			var economy = Economy(("Gold", 0));

			var result = new EconomyValidator().CanAfford(economy, "Gold", 0);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_Single_NegativeAmount_ReturnsOk()
		{
			var economy = Economy(("Gold", 0));

			var result = new EconomyValidator().CanAfford(economy, "Gold", -10);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanAfford_Single_NullEconomy_ReturnsFail()
		{
			var result = new EconomyValidator().CanAfford(null, "Gold", 10);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("null"));
		}

		[Test]
		public void CanAfford_Single_NullResourceType_ReturnsFail()
		{
			var economy = Economy(("Gold", 100));

			var result = new EconomyValidator().CanAfford(economy, null, 10);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("Tipo risorsa"));
		}

		[Test]
		public void CanAfford_Single_EmptyResourceType_ReturnsFail()
		{
			var economy = Economy(("Gold", 100));

			var result = new EconomyValidator().CanAfford(economy, "", 10);

			Assert.IsFalse(result.IsValid);
		}

		#endregion

		#region CanCollect — cooldown

		[Test]
		public void CanCollect_ZeroCooldown_ReturnsOkImmediately()
		{
			var building = Building(cooldownSeconds: 0);
			var lastCollected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var now = lastCollected.AddSeconds(0);

			var result = new EconomyValidator().CanCollect(building, lastCollected, now);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanCollect_CooldownElapsed_ReturnsOk()
		{
			var building = Building(cooldownSeconds: 60);
			var lastCollected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var now = lastCollected.AddSeconds(60);

			var result = new EconomyValidator().CanCollect(building, lastCollected, now);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanCollect_CooldownExceeded_ReturnsOk()
		{
			var building = Building(cooldownSeconds: 60);
			var lastCollected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var now = lastCollected.AddSeconds(120);

			var result = new EconomyValidator().CanCollect(building, lastCollected, now);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanCollect_CooldownNotElapsed_ReturnsFailWithResidual()
		{
			var building = Building(cooldownSeconds: 60);
			var lastCollected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var now = lastCollected.AddSeconds(30);

			var result = new EconomyValidator().CanCollect(building, lastCollected, now);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("Cooldown"));
			Assert.IsTrue(result.Reason.Contains("30"));
		}

		[Test]
		public void CanCollect_JustBeforeCooldown_ReturnsFail()
		{
			var building = Building(cooldownSeconds: 60);
			var lastCollected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var now = lastCollected.AddSeconds(59.9);

			var result = new EconomyValidator().CanCollect(building, lastCollected, now);

			Assert.IsFalse(result.IsValid);
		}

		[Test]
		public void CanCollect_LastCollectedInFuture_ReturnsFail()
		{
			var building = Building(cooldownSeconds: 60);
			var lastCollected = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
			var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			var result = new EconomyValidator().CanCollect(building, lastCollected, now);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("futuro"));
		}

		[Test]
		public void CanCollect_NullBuilding_ReturnsFail()
		{
			var lastCollected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			var result = new EconomyValidator().CanCollect(null, lastCollected, lastCollected);

			Assert.IsFalse(result.IsValid);
			Assert.IsTrue(result.Reason.Contains("null"));
		}

		[Test]
		public void CanCollect_NegativeCooldown_ReturnsOk()
		{
			var building = Building(cooldownSeconds: -10);
			var lastCollected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			var result = new EconomyValidator().CanCollect(building, lastCollected, lastCollected);

			Assert.IsTrue(result.IsValid);
		}

		[Test]
		public void CanCollect_DefaultNow_UsesUtcNow()
		{
			var building = Building(cooldownSeconds: 0);
			var lastCollected = DateTime.UtcNow.AddSeconds(-1);

			var result = new EconomyValidator().CanCollect(building, lastCollected);

			Assert.IsTrue(result.IsValid);
		}

		#endregion
	}
}
