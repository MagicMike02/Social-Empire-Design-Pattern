using System;
using System.Collections.Generic;
using System.Text;
using Script.Core.DTO;

namespace Script.EconomySystem.Validation
{
	/// <summary>
	/// Validatore puro per le operazioni economiche del giocatore.
	/// NESSUNA dipendenza da UnityEngine/MonoBehaviour — utilizzabile
	/// sia lato client (optimistic update) sia lato server (validazione autoritativa).
	/// </summary>
	/// <remarks>
	/// Estrae e rende testabile la logica di <c>GameEconomyManager.CanAfford</c>
	/// e la validazione del cooldown di raccolta risorse.
	/// Opera esclusivamente su DTO immutabili.
	/// </remarks>
	public sealed class EconomyValidator
	{
		#region Public API — CanAfford

		/// <summary>
		/// Verifica se il giocatore può permettersi una serie di costi multi-risorsa.
		/// Equivalente puro di <see cref="Script.EconomySystem.GameEconomyManager.CanAfford"/>.
		/// </summary>
		/// <param name="economy">Snapshot immutabile dello stato economico del giocatore.</param>
		/// <param name="costs">Costi per tipo di risorsa (chiave = nome enum ResourceType).</param>
		/// <returns>Ok se tutti i costi sono coperti; Fail con dettaglio della prima risorsa mancante.</returns>
		public ValidationResult CanAfford(PlayerEconomyData economy, IReadOnlyDictionary<string, int> costs)
		{
			if (economy is null)
				return ValidationResult.Fail("Stato economia nullo.");
			if (costs is null || costs.Count == 0)
				return ValidationResult.Ok();

			var missing = new List<string>();
			foreach (var cost in costs)
			{
				if (cost.Value <= 0)
					continue;

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

		/// <summary>
		/// Verifica se il giocatore può permettersi una spesa singola.
		/// Equivalente puro dell'overload
		/// <see cref="Script.EconomySystem.GameEconomyManager.CanAfford(ResourceType, int)"/>.
		/// </summary>
		/// <param name="economy">Snapshot immutabile dello stato economico del giocatore.</param>
		/// <param name="resourceType">Nome dell'enum ResourceType della risorsa da spendere.</param>
		/// <param name="amount">Quantità da spendere (≤ 0 = sempre valida).</param>
		public ValidationResult CanAfford(PlayerEconomyData economy, string resourceType, int amount)
		{
			if (economy is null)
				return ValidationResult.Fail("Stato economia nullo.");
			if (string.IsNullOrEmpty(resourceType))
				return ValidationResult.Fail("Tipo risorsa nullo o vuoto.");
			if (amount <= 0)
				return ValidationResult.Ok();

			int balance = economy.Resources.TryGetValue(resourceType, out var value) ? value : 0;
			if (balance < amount)
				return ValidationResult.Fail(
					$"Risorse insufficienti per {resourceType}: serve {amount}, ha {balance}.");

			return ValidationResult.Ok();
		}

		#endregion

		#region Public API — CanCollect

		/// <summary>
		/// Verifica se un edificio può raccogliere risorse prodotte al momento
		/// della chiamata, rispettando il cooldown di raccolta.
		/// </summary>
		/// <param name="building">Configurazione dell'edificio (contiene il cooldown).</param>
		/// <param name="lastCollected">Timestamp UTC dell'ultima raccolta.</param>
		/// <param name="now">Timestamp UTC corrente (iniettabile per test).</param>
		/// <returns>
		/// Ok se il cooldown è trascorso (o nullo); Fail con tempo residuo altrimenti.
		/// </returns>
		public ValidationResult CanCollect(
			BuildingConfigData building,
			DateTime lastCollected,
			DateTime? now = null)
		{
			if (building is null)
				return ValidationResult.Fail("Configurazione edificio nulla.");

			// Cooldown nullo o zero = raccolta immediata sempre consentita.
			if (building.CollectCooldownSeconds <= 0)
				return ValidationResult.Ok();

			DateTime currentUtc = now ?? DateTime.UtcNow;
			TimeSpan elapsed = currentUtc - lastCollected;

			if (elapsed < TimeSpan.Zero)
				return ValidationResult.Fail(
					"Timestamp ultima raccolta nel futuro (orologio sistema modificato?).");

			TimeSpan cooldown = TimeSpan.FromSeconds(building.CollectCooldownSeconds);
			if (elapsed < cooldown)
			{
				TimeSpan remaining = cooldown - elapsed;
				return ValidationResult.Fail(
					$"Cooldown non trascorso: residuo {remaining.TotalSeconds:F1}s " +
					$"su {building.CollectCooldownSeconds:F0}s.");
			}

			return ValidationResult.Ok();
		}

		#endregion
	}
}
