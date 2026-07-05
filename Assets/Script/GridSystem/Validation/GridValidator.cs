using System.Collections.Generic;
using System.Text;
using Script.Core.DTO;

namespace Script.GridSystem.Validation
{
	/// <summary>
	/// Validatore puro per le operazioni sulla griglia di gioco.
	/// NESSUNA dipendenza da UnityEngine/MonoBehaviour — utilizzabile
	/// sia lato client (optimistic update) sia lato server (validazione autoritativa).
	/// </summary>
	/// <remarks>
	/// Estrae e rende testabile la logica di <c>GridManager.IsValidCell</c>,
	/// <c>GridManager.IsCellFree</c> e <c>GridManager.AreCellsFree</c>.
	/// Opera esclusivamente su <see cref="GridStateData"/> immutabile.
	/// </remarks>
	public sealed class GridValidator
	{
		#region Public API — IsCellInBounds

		/// <summary>
		/// Verifica che una cella sia all'interno dei limiti della griglia.
		/// Equivalente puro di <c>GridManager.IsValidCell</c>.
		/// </summary>
		/// <param name="grid">Snapshot immutabile dello stato della griglia.</param>
		/// <param name="x">Coordinata X (colonna).</param>
		/// <param name="y">Coordinata Y (riga).</param>
		public ValidationResult IsCellInBounds(GridStateData grid, int x, int y)
		{
			if (grid is null)
				return ValidationResult.Fail("Stato griglia nullo.");

			if (x < 0 || x >= grid.Width || y < 0 || y >= grid.Height)
				return ValidationResult.Fail(
					$"Cella ({x},{y}) fuori dai limiti della griglia {grid.Width}×{grid.Height}.");

			return ValidationResult.Ok();
		}

		#endregion

		#region Public API — IsCellEmpty

		/// <summary>
		/// Verifica che una cella sia libera: in bounds, sbloccata e non occupata
		/// da un edificio. Equivalente puro di <c>GridManager.IsCellFree</c>
		/// combinato con il check di sblocco di <c>GridManager.AreCellsFree</c>.
		/// </summary>
		/// <param name="grid">Snapshot immutabile dello stato della griglia.</param>
		/// <param name="x">Coordinata X (colonna).</param>
		/// <param name="y">Coordinata Y (riga).</param>
		public ValidationResult IsCellEmpty(GridStateData grid, int x, int y)
		{
			// Bounds check (riutilizza IsCellInBounds per coerenza)
			var boundsResult = IsCellInBounds(grid, x, y);
			if (!boundsResult.IsValid)
				return boundsResult;

			// Unlock check: la cella deve essere sbloccata
			if (!grid.UnlockedCells.Contains((x, y)))
				return ValidationResult.Fail(
					$"Cella ({x},{y}) non è sbloccata (zona bloccata o inesplorata).");

			// Occupancy check: nessun edificio deve occupare la cella
			if (grid.PlacedBuildings.ContainsKey((x, y)))
				return ValidationResult.Fail(
					$"Cella ({x},{y}) già occupata dall'edificio '{grid.PlacedBuildings[(x, y)]}'.");

			return ValidationResult.Ok();
		}

		#endregion

		#region Public API — CanPlaceShape

		/// <summary>
		/// Verifica che un'area rettangolare w×h con origine in (x,y) sia
		/// completamente libera per il piazzamento di un edificio.
		/// Equivalente puro di <c>GridManager.AreCellsFree</c>.
		/// </summary>
		/// <param name="grid">Snapshot immutabile dello stato della griglia.</param>
		/// <param name="x">Coordinata X dell'angolo di origine.</param>
		/// <param name="y">Coordinata Y dell'angolo di origine.</param>
		/// <param name="w">Larghezza in celle dell'edificio.</param>
		/// <param name="h">Altezza in celle dell'edificio.</param>
		/// <returns>
		/// Ok se TUTTE le celle dell'area sono in bounds, sbloccate e libere;
		/// Fail con dettaglio della prima cella problematica.
		/// </returns>
		public ValidationResult CanPlaceShape(GridStateData grid, int x, int y, int w, int h)
		{
			if (grid is null)
				return ValidationResult.Fail("Stato griglia nullo.");

			if (w <= 0 || h <= 0)
				return ValidationResult.Fail(
					$"Dimensioni edificio non valide: {w}×{h} (devono essere > 0).");

			// Verifica bounds dell'intera area in un colpo solo (early exit)
			if (x < 0 || y < 0 || x + w > grid.Width || y + h > grid.Height)
				return ValidationResult.Fail(
					$"Area {w}×{h} con origine ({x},{y}) fuoriesce dai limiti " +
					$"della griglia {grid.Width}×{grid.Height}.");

			// Itera ogni cella dell'area: unlock + occupancy
			var blockedCells = new List<string>();
			for (int dx = 0; dx < w; dx++)
			{
				for (int dy = 0; dy < h; dy++)
				{
					int cx = x + dx;
					int cy = y + dy;
					var coord = (cx, cy);

					if (!grid.UnlockedCells.Contains(coord))
					{
						blockedCells.Add($"({cx},{cy}): non sbloccata");
						continue;
					}

					if (grid.PlacedBuildings.TryGetValue(coord, out var buildingId))
						blockedCells.Add($"({cx},{cy}): occupata da '{buildingId}'");
				}
			}

			if (blockedCells.Count > 0)
			{
				var sb = new StringBuilder("Area non completamente libera: ");
				sb.Append(string.Join("; ", blockedCells));
				sb.Append('.');
				return ValidationResult.Fail(sb.ToString());
			}

			return ValidationResult.Ok();
		}

		#endregion
	}
}