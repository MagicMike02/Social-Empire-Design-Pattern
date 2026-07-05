using System.Collections.Generic;
using NUnit.Framework;
using Script.Core.DTO;
using Script.GridSystem.Validation;

namespace Tests.EditMode.Validation
{
	[TestFixture]
	public class GridValidatorTests
	{
		private GridValidator _validator;

		[SetUp]
		public void SetUp()
		{
			_validator = new GridValidator();
		}

		#region Helpers

		private static GridStateData Grid(int w, int h,
			Dictionary<(int, int), string> buildings = null,
			HashSet<(int, int)> unlocked = null)
		{
			return new GridStateData(w, h,
				buildings ?? new Dictionary<(int, int), string>(),
				unlocked ?? new HashSet<(int, int)>());
		}

		private static GridStateData FullyUnlocked(int w, int h)
			=> GridStateData.FullyUnlocked(w, h);

		#endregion

		#region IsCellInBounds

		[Test]
		public void IsCellInBounds_Origine_Ok()
		{
			var grid = Grid(10, 10);
			var result = _validator.IsCellInBounds(grid, 0, 0);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void IsCellInBounds_AngoloOpposto_Ok()
		{
			var grid = Grid(10, 10);
			var result = _validator.IsCellInBounds(grid, 9, 9);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void IsCellInBounds_Centro_Ok()
		{
			var grid = Grid(10, 10);
			var result = _validator.IsCellInBounds(grid, 5, 5);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void IsCellInBounds_XNegativo_Fail()
		{
			var grid = Grid(10, 10);
			var result = _validator.IsCellInBounds(grid, -1, 5);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuori dai limiti", result.Reason);
		}

		[Test]
		public void IsCellInBounds_YNegativo_Fail()
		{
			var grid = Grid(10, 10);
			var result = _validator.IsCellInBounds(grid, 5, -1);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuori dai limiti", result.Reason);
		}

		[Test]
		public void IsCellInBounds_XOltreLimite_Fail()
		{
			var grid = Grid(10, 10);
			var result = _validator.IsCellInBounds(grid, 10, 5);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuori dai limiti", result.Reason);
		}

		[Test]
		public void IsCellInBounds_YOltreLimite_Fail()
		{
			var grid = Grid(10, 10);
			var result = _validator.IsCellInBounds(grid, 5, 10);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuori dai limiti", result.Reason);
		}

		[Test]
		public void IsCellInBounds_GridNullo_Fail()
		{
			var result = _validator.IsCellInBounds(null, 0, 0);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("nullo", result.Reason);
		}

		[Test]
		public void IsCellInBounds_Griglia1x1_Ok()
		{
			var grid = Grid(1, 1);
			var result = _validator.IsCellInBounds(grid, 0, 0);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		#endregion

		#region IsCellEmpty

		[Test]
		public void IsCellEmpty_CellaLiberaESbloccata_Ok()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.IsCellEmpty(grid, 3, 4);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void IsCellEmpty_FuoriBounds_Fail()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.IsCellEmpty(grid, -1, 5);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuori dai limiti", result.Reason);
		}

		[Test]
		public void IsCellEmpty_CellaNonSbloccata_Fail()
		{
			var grid = Grid(10, 10); // nessuna cella unlocked
			var result = _validator.IsCellEmpty(grid, 3, 4);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("non è sbloccata", result.Reason);
		}

		[Test]
		public void IsCellEmpty_CellaOccupataDaEdificio_Fail()
		{
			var buildings = new Dictionary<(int, int), string>
			{
				[(3, 4)] = "house"
			};
			var grid = Grid(10, 10, buildings, FullyUnlocked(10, 10).UnlockedCells);
			var result = _validator.IsCellEmpty(grid, 3, 4);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("occupata", result.Reason);
			StringAssert.Contains("house", result.Reason);
		}

		[Test]
		public void IsCellEmpty_GridNullo_Fail()
		{
			var result = _validator.IsCellEmpty(null, 0, 0);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("nullo", result.Reason);
		}

		[Test]
		public void IsCellEmpty_SoloAlcuneCelleSbloccate_TargetSbloccata_Ok()
		{
			var unlocked = new HashSet<(int, int)> { (5, 5), (5, 6) };
			var grid = Grid(10, 10, null, unlocked);
			var result = _validator.IsCellEmpty(grid, 5, 5);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void IsCellEmpty_SoloAlcuneCelleSbloccate_TargetBloccata_Fail()
		{
			var unlocked = new HashSet<(int, int)> { (5, 5) };
			var grid = Grid(10, 10, null, unlocked);
			var result = _validator.IsCellEmpty(grid, 5, 6);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("non è sbloccata", result.Reason);
		}

		#endregion

		#region CanPlaceShape

		[Test]
		public void CanPlaceShape_1x1_AreaLibera_Ok()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 3, 4, 1, 1);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanPlaceShape_2x2_AreaLibera_Ok()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 1, 1, 2, 2);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanPlaceShape_3x3_Angolo_Ok()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 0, 0, 3, 3);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanPlaceShape_FuoriBounds_Destra_Fail()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 8, 0, 3, 2);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuoriesce dai limiti", result.Reason);
		}

		[Test]
		public void CanPlaceShape_FuoriBounds_Alto_Fail()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 0, 9, 2, 2);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuoriesce dai limiti", result.Reason);
		}

		[Test]
		public void CanPlaceShape_FuoriBounds_OrigineNegativa_Fail()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, -1, 0, 2, 2);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("fuoriesce dai limiti", result.Reason);
		}

		[Test]
		public void CanPlaceShape_CellaOccupata_Fail()
		{
			var buildings = new Dictionary<(int, int), string>
			{
				[(2, 2)] = "house"
			};
			var grid = Grid(10, 10, buildings, FullyUnlocked(10, 10).UnlockedCells);
			var result = _validator.CanPlaceShape(grid, 1, 1, 3, 3);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("occupata da 'house'", result.Reason);
		}

		[Test]
		public void CanPlaceShape_CellaNonSbloccata_Fail()
		{
			var unlocked = FullyUnlocked(10, 10).UnlockedCells;
			unlocked.Remove((1, 1)); // rimuovi una cella dall'area
			var grid = Grid(10, 10, null, unlocked);
			var result = _validator.CanPlaceShape(grid, 0, 0, 2, 2);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("non sbloccata", result.Reason);
		}

		[Test]
		public void CanPlaceShape_DimensioniZero_Fail()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 0, 0, 0, 1);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("non valide", result.Reason);
		}

		[Test]
		public void CanPlaceShape_DimensioniNegative_Fail()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 0, 0, -1, 2);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("non valide", result.Reason);
		}

		[Test]
		public void CanPlaceShape_GridNullo_Fail()
		{
			var result = _validator.CanPlaceShape(null, 0, 0, 1, 1);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("nullo", result.Reason);
		}

		[Test]
		public void CanPlaceShape_EsattamenteAiBordi_Ok()
		{
			var grid = FullyUnlocked(10, 10);
			var result = _validator.CanPlaceShape(grid, 7, 7, 3, 3);
			Assert.IsTrue(result.IsValid, result.Reason);
		}

		[Test]
		public void CanPlaceShape_MultipleProblemi_ElencaTutti()
		{
			var unlocked = FullyUnlocked(10, 10).UnlockedCells;
			unlocked.Remove((1, 1)); // non sbloccata
			var buildings = new Dictionary<(int, int), string>
			{
				[(2, 2)] = "barracks"
			};
			var grid = Grid(10, 10, buildings, unlocked);
			var result = _validator.CanPlaceShape(grid, 1, 1, 2, 2);
			Assert.IsFalse(result.IsValid);
			StringAssert.Contains("non sbloccata", result.Reason);
			StringAssert.Contains("occupata da 'barracks'", result.Reason);
		}

		#endregion
	}
}