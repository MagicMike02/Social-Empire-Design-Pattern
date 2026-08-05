using System.Collections.Generic;
using System.Text;
using Script.Core.DTO;

namespace Script.BuildingSystem.Validation
{
	/// <summary>
	/// Validatore puro per il piazzamento e la distruzione di edifici.
	/// NESSUNA dipendenza da UnityEngine/MonoBehaviour — utilizzabile
	/// sia lato client (optimistic update) sia lato server (validazione autoritativa).
	/// </summary>
	/// <remarks>
	/// Il validatore opera esclusivamente su DTO immutabili, non accede a MonoBehaviour.
	/// </remarks>
	public sealed class BuildingValidator
	{
		#region Public API

		/// <summary>
		/// Verifica se un edificio può essere piazzato alla coordinata (x, y).
		/// Controlli nell'ordine: 1) argomenti validi, 2) bounds griglia,
		/// 3) celle sbloccate, 4) celle libere, 5) risorse sufficienti.
		/// </summary>
		/// <returns>ValidationResult con motivo in caso di fallimento.</returns>
		public ValidationResult CanPlace(
			BuildingConfigData config,
			PlayerEconomyData economy,
			GridStateData grid,
			int x,
			int y)
		{
			// Controllo 0: argomenti
			if (config is null)
				return ValidationResult.Fail("Configurazione edificio nulla.");
			if (grid is null)
				return ValidationResult.Fail("Stato griglia nullo.");
			if (economy is null)
				return ValidationResult.Fail("Stato economia nullo.");

			// Controllo dimensioni edificio positive
			if (config.Width <= 0 || config.Height <= 0)
				return ValidationResult.Fail($"Dimensioni edificio non valide: {config.Width}x{config.Height}.");

			// Controllo 1: bounds griglia (rettangolo completo dentro)
			var bounds = CheckBounds(grid, x, y, config.Width, config.Height);
			if (!bounds.IsValid) return bounds;

			// Controllo 2: celle sbloccate (tile state Unlocked)
			var unlocked = CheckCellsUnlocked(grid, x, y, config.Width, config.Height);
			if (!unlocked.IsValid) return unlocked;

			// Controllo 3: celle libere (non occupate da altri edifici)
			var free = CheckCellsFree(grid, x, y, config.Width, config.Height);
			if (!free.IsValid) return free;

			// Controllo 4: risorse sufficienti
			var afford = CheckAffordable(config, economy);
			if (!afford.IsValid) return afford;

			return ValidationResult.Ok();
		}

		/// <summary>
		/// Verifica se un edificio può essere distrutto.
		/// </summary>
		/// <param name="buildingId">Identificativo dell'istanza edificio piazzata.</param>
		/// <param name="grid">Stato corrente della griglia.</param>
		public ValidationResult CanDestroy(string buildingId, GridStateData grid)
		{
			if (grid is null)
				return ValidationResult.Fail("Stato griglia nullo.");
			if (string.IsNullOrEmpty(buildingId))
				return ValidationResult.Fail("Identificativo edificio nullo o vuoto.");

			// Verifica che l'edificio esista sulla griglia.
			// IReadOnlyDictionary non espone ContainsValue: iterazione manuale.
			bool exists = false;
			foreach (var placedId in grid.PlacedBuildings.Values)
			{
				if (placedId == buildingId)
				{
					exists = true;
					break;
				}
			}
			if (!exists)
				return ValidationResult.Fail($"Edificio '{buildingId}' non presente sulla griglia.");

			return ValidationResult.Ok();
		}

		#endregion

		#region Private Checks

		private static ValidationResult CheckBounds(GridStateData grid, int x, int y, int width, int height)
		{
			if (x < 0 || y < 0)
				return ValidationResult.Fail($"Coordinate negative: ({x},{y}).");

			if (x + width > grid.Width || y + height > grid.Height)
				return ValidationResult.Fail(
					$"Fuori bounds: rettangolo [{x},{y}]-[{x + width - 1},{y + height - 1}] " +
					$"eccede griglia {grid.Width}x{grid.Height}.");

			return ValidationResult.Ok();
		}

		private static ValidationResult CheckCellsUnlocked(GridStateData grid, int x, int y, int width, int height)
		{
			for (int dx = 0; dx < width; dx++)
			{
				for (int dy = 0; dy < height; dy++)
				{
					var coord = (x + dx, y + dy);
					if (!grid.UnlockedCells.Contains(coord))
						return ValidationResult.Fail(
							$"Cella ({coord.Item1},{coord.Item2}) non sbloccata.");
				}
			}
			return ValidationResult.Ok();
		}

		private static ValidationResult CheckCellsFree(GridStateData grid, int x, int y, int width, int height)
		{
			for (int dx = 0; dx < width; dx++)
			{
				for (int dy = 0; dy < height; dy++)
				{
					var coord = (x + dx, y + dy);
					if (grid.PlacedBuildings.ContainsKey(coord))
						return ValidationResult.Fail(
							$"Cella ({coord.Item1},{coord.Item2}) occupata da un altro edificio.");
				}
			}
			return ValidationResult.Ok();
		}

		private static ValidationResult CheckAffordable(BuildingConfigData config, PlayerEconomyData economy)
		{
			if (config.Costs is null || config.Costs.Count == 0)
				return ValidationResult.Ok();

			var missing = new List<string>();
			foreach (var cost in config.Costs)
			{
				int balance = economy.Resources.TryGetValue(cost.Key, out var amount) ? amount : 0;
				if (balance < cost.Value)
					missing.Add($"{cost.Key}: serve {cost.Value}, ha {balance}");
			}

			if (missing.Count > 0)
			{
				var sb = new StringBuilder("Risorse insufficienti: ");
				sb.Append(string.Join("; ", missing));
				sb.Append('.');
				return ValidationResult.Fail(sb.ToString());
			}

			return ValidationResult.Ok();
		}

		#endregion
	}
}
