using System.Collections.Generic;
using NUnit.Framework;
using Script.BuildingSystem.Validation;
using Script.Core.DTO;

namespace Tests.EditMode.Validation
{
	[TestFixture]
	public class BuildingValidatorTests
	{
		private BuildingValidator _validator;

		[SetUp]
		public void SetUp()
		{
			_validator = new BuildingValidator();
		}

		#region CanPlace — casi positivi

		[Test]
		public void CanPlace_Edificio1x1_CellaLiberaESbloccata_RisorseSufficienti_Ok()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = BuildingConfigData.Single("house", "Gold", 100);

			var result = _validator.CanPlace(config, economy, grid, 3, 4);

			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanPlace_Edificio2x2_AreaLibera_Ok()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = new BuildingConfigData("barracks", 2, 2,
				new Dictionary<string, int> { ["Gold"] = 200 });

			var result = _validator.CanPlace(config, economy, grid, 1, 1);

			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanPlace_CostiMultiRisorsa_TuttiSoddisfatti_Ok()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = new PlayerEconomyData(new Dictionary<string, int>
			{
				["Gold"] = 300,
				["Wood"] = 150,
				["Stone"] = 80
			});
			var config = new BuildingConfigData("townhall", 2, 2,
				new Dictionary<string, int>
				{
					["Gold"] = 200,
					["Wood"] = 100,
					["Stone"] = 50
				});

			var result = _validator.CanPlace(config, economy, grid, 0, 0);

			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanPlace_CostiNulli_SenzaRisorse_Ok()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = new PlayerEconomyData(new Dictionary<string, int>());
			var config = new BuildingConfigData("decoration", 1, 1);

			var result = _validator.CanPlace(config, economy, grid, 5, 5);

			Assert.IsTrue(result.IsValid, result.Reason);
		}

		#endregion

		#region CanPlace — bounds

		[Test]
		public void CanPlace_CoordinateNegative_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = BuildingConfigData.Single("house", "Gold", 100);

			var result = _validator.CanPlace(config, economy, grid, -1, 0);

			Assert.IsFalse(result.IsValid);
			Assert.IsNotNull(result.Reason);
			StringAssert.Contains("negative", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_FuoriBounds_Destra_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = new BuildingConfigData("house", 2, 1, new Dictionary<string, int> { ["Gold"] = 100 });

			// x=9, width=2 → eccede (9+2=11 > 10)
			var result = _validator.CanPlace(config, economy, grid, 9, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("bounds", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_EdificioBordoEsatto_Ok()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = new BuildingConfigData("house", 2, 2, new Dictionary<string, int> { ["Gold"] = 100 });

			// x=8, width=2 → 8+2=10 == Width → ok
			var result = _validator.CanPlace(config, economy, grid, 8, 8);

			Assert.IsTrue(result.IsValid, result.Reason);
		}

		#endregion

		#region CanPlace — celle occupate

		[Test]
		public void CanPlace_CellaOccupata_Fail()
		{
			var placed = new Dictionary<(int, int), string> { [(3, 4)] = "existing_house" };
			var unlocked = new HashSet<(int, int)>();
			for (int x = 0; x < 10; x++)
				for (int y = 0; y < 10; y++)
					unlocked.Add((x, y));
			var grid = new GridStateData(10, 10, placed, unlocked);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = BuildingConfigData.Single("house", "Gold", 100);

			var result = _validator.CanPlace(config, economy, grid, 3, 4);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("occupata", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_Edificio2x2_SovrapponeParzialmente_Fail()
		{
			var placed = new Dictionary<(int, int), string> { [(2, 2)] = "tower" };
			var unlocked = new HashSet<(int, int)>();
			for (int x = 0; x < 10; x++)
				for (int y = 0; y < 10; y++)
					unlocked.Add((x, y));
			var grid = new GridStateData(10, 10, placed, unlocked);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = new BuildingConfigData("barracks", 2, 2,
				new Dictionary<string, int> { ["Gold"] = 200 });

			// origin (1,1) → celle (1,1),(2,1),(1,2),(2,2) → (2,2) occupata
			var result = _validator.CanPlace(config, economy, grid, 1, 1);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("occupata", result.Reason.ToLower());
		}

		#endregion

		#region CanPlace — celle bloccate

		[Test]
		public void CanPlace_CellaBloccata_Fail()
		{
			// Griglia 10x10, NESSUNA cella sbloccata
			var grid = GridStateData.Empty(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = BuildingConfigData.Single("house", "Gold", 100);

			var result = _validator.CanPlace(config, economy, grid, 3, 4);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("sbloccata", result.Reason.ToLower());
		}

		#endregion

		#region CanPlace — risorse

		[Test]
		public void CanPlace_RisorseInsufficienti_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 50);
			var config = BuildingConfigData.Single("house", "Gold", 100);

			var result = _validator.CanPlace(config, economy, grid, 0, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("insufficienti", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_RisorsaMancanteNelSaldo_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			// Saldo ha solo Gold, config richiede Wood
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = BuildingConfigData.Single("sawmill", "Wood", 100);

			var result = _validator.CanPlace(config, economy, grid, 0, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("wood", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_CostiMultiRisorsa_Parziale_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = new PlayerEconomyData(new Dictionary<string, int>
			{
				["Gold"] = 300,
				["Wood"] = 50   // serve 100
			});
			var config = new BuildingConfigData("townhall", 2, 2,
				new Dictionary<string, int>
				{
					["Gold"] = 200,
					["Wood"] = 100
				});

			var result = _validator.CanPlace(config, economy, grid, 0, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("wood", result.Reason.ToLower());
		}

		#endregion

		#region CanPlace — argomenti nulli

		[Test]
		public void CanPlace_ConfigNull_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);

			var result = _validator.CanPlace(null, economy, grid, 0, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("config", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_GridNull_Fail()
		{
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = BuildingConfigData.Single("house", "Gold", 100);

			var result = _validator.CanPlace(config, economy, null, 0, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("griglia", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_EconomyNull_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var config = BuildingConfigData.Single("house", "Gold", 100);

			var result = _validator.CanPlace(config, null, grid, 0, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("economia", result.Reason.ToLower());
		}

		[Test]
		public void CanPlace_DimensioniNonPositive_Fail()
		{
			var grid = GridStateData.FullyUnlocked(10, 10);
			var economy = PlayerEconomyData.Of("Gold", 500);
			var config = new BuildingConfigData("bad", 0, 1);

			var result = _validator.CanPlace(config, economy, grid, 0, 0);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("dimensioni", result.Reason.ToLower());
		}

		#endregion

		#region CanDestroy

		[Test]
		public void CanDestroy_EdificioEsistente_Ok()
		{
			var placed = new Dictionary<(int, int), string> { [(3, 4)] = "house_1" };
			var grid = new GridStateData(10, 10, placed);

			var result = _validator.CanDestroy("house_1", grid);

			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanDestroy_EdificioInesistente_Fail()
		{
			var grid = GridStateData.Empty(10, 10);

			var result = _validator.CanDestroy("ghost", grid);

			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("non presente", result.Reason.ToLower());
		}

		[Test]
		public void CanDestroy_IdNullo_Fail()
		{
			var grid = GridStateData.Empty(10, 10);

			var result = _validator.CanDestroy(null, grid);

			Assert.IsFalse(result.IsValid);
		}

		[Test]
		public void CanDestroy_IdVuoto_Fail()
		{
			var grid = GridStateData.Empty(10, 10);

			var result = _validator.CanDestroy("", grid);

			Assert.IsFalse(result.IsValid);
		}

		[Test]
		public void CanDestroy_GridNull_Fail()
		{
			var result = _validator.CanDestroy("house_1", null);

			Assert.IsFalse(result.IsValid);
		}

		#endregion

		#region ValidationResult — composizione

		[Test]
		public void ValidationResult_Ok_IsValidTrue_ReasonNull()
		{
			var r = ValidationResult.Ok();
			Assert.IsTrue(r.IsValid);
			Assert.IsNull(r.Reason);
		}

		[Test]
		public void ValidationResult_Fail_IsValidFalse_ReasonSet()
		{
			var r = ValidationResult.Fail("motivo");
			Assert.IsFalse(r.IsValid);
			Assert.AreEqual("motivo", r.Reason);
		}

		[Test]
		public void ValidationResult_And_PrimoFallitoVince()
		{
			var ok = ValidationResult.Ok();
			var fail = ValidationResult.Fail("x");

			var combined = ok & fail;
			Assert.IsFalse(combined.IsValid);
			Assert.AreEqual("x", combined.Reason);
		}

		[Test]
		public void ValidationResult_And_EntrambiOk_Ok()
		{
			var a = ValidationResult.Ok();
			var b = ValidationResult.Ok();

			var combined = a & b;
			Assert.IsTrue(combined.IsValid);
		}

		#endregion
	}
}
