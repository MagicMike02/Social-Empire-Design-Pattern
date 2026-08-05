using System;
using System.Collections.Generic;
using System.Globalization;
using Script.ResourceSystem.Enums;

namespace Script.Core.SaveSystem
{
	/// <summary>
	/// Calcola i progressi offline (risorse accumulate) tra l'ultima uscita dal gioco
	/// e il ritorno del giocatore.
	///
	/// Classe C# pura: nessuna dipendenza UnityEngine, riutilizzabile lato server
	/// per validazione autoritativa.
	/// </summary>
	public sealed class OfflineProgressCalculator
	{
		#region Constants

		/// <summary>
		/// Cap massimo di ore offline calcolabili.
		/// Oltre questa soglia il calcolo viene limitato.
		/// </summary>
		private const double MaxOfflineHoursCap = 8.0;

		/// <summary>
		/// Production rate di default (unità/ora).
		/// </summary>
		private static readonly IReadOnlyDictionary<ResourceType, double> DefaultProductionRates =
			new Dictionary<ResourceType, double>
			{
				{ ResourceType.Wood, 10.0 },
				{ ResourceType.Stone, 8.0 },
				{ ResourceType.Gold, 5.0 },
				{ ResourceType.Meat, 6.0 }
                // Experience non produce risorse passive
            };

		#endregion

		#region Fields

		private readonly IReadOnlyDictionary<ResourceType, double> _productionRates;

		#endregion

		#region Constructors

		/// <summary>
		/// Costruisce il calcolatore utilizzando i production rate di default.
		/// </summary>
		public OfflineProgressCalculator()
			: this(DefaultProductionRates)
		{
		}

		/// <summary>
		/// Costruisce il calcolatore utilizzando production rate personalizzati.
		/// </summary>
		public OfflineProgressCalculator(IReadOnlyDictionary<ResourceType, double> productionRates)
		{
			_productionRates = productionRates != null
				? new Dictionary<ResourceType, double>(productionRates)
				: new Dictionary<ResourceType, double>(DefaultProductionRates);
		}

		#endregion

		#region Public API

		/// <summary>
		/// Calcola i progressi offline utilizzando l'orario UTC corrente.
		/// </summary>
		public OfflineProgressResult Calculate(GameSaveData saveData)
		{
			return Calculate(saveData, DateTime.UtcNow);
		}

		/// <summary>
		/// Calcola i progressi offline utilizzando un orario UTC specificato.
		/// Utile per test automatici o validazione lato server.
		/// </summary>
		public OfflineProgressResult Calculate(GameSaveData saveData, DateTime currentUtc)
		{
			if (saveData == null)
			{
				return OfflineProgressResult.Empty("saveData null.");
			}

			if (string.IsNullOrWhiteSpace(saveData.lastExitAt))
			{
				return OfflineProgressResult.Empty("lastExitAt assente.");
			}

			if (!DateTime.TryParse(
					saveData.lastExitAt,
					null,
					DateTimeStyles.RoundtripKind,
					out DateTime lastExitUtc))
			{
				return OfflineProgressResult.Empty(
					$"lastExitAt non valido: '{saveData.lastExitAt}'.");
			}

			TimeSpan elapsed = currentUtc - lastExitUtc;

			if (elapsed <= TimeSpan.Zero)
			{
				return OfflineProgressResult.Empty(
					"Elapsed <= 0 (orologio sistema modificato?).");
			}

			double elapsedHours = elapsed.TotalHours;
			bool wasCapped = false;

			if (elapsedHours > MaxOfflineHoursCap)
			{
				elapsedHours = MaxOfflineHoursCap;
				wasCapped = true;
			}

			Dictionary<ResourceType, int> deltas = new();

			foreach (KeyValuePair<ResourceType, double> kvp in _productionRates)
			{
				if (kvp.Value <= 0)
					continue;

				int produced = (int)Math.Floor(kvp.Value * elapsedHours);

				if (produced > 0)
				{
					deltas[kvp.Key] = produced;
				}
			}

			return new OfflineProgressResult(
				deltas,
				elapsed,
				elapsedHours,
				wasCapped,
				true,
				null);
		}

		#endregion
	}

	/// <summary>
	/// Risultato del calcolo dei progressi offline.
	/// Classe immutabile e indipendente da Unity.
	/// </summary>
	public sealed class OfflineProgressResult
	{
		public IReadOnlyDictionary<ResourceType, int> Deltas { get; }
		public TimeSpan Elapsed { get; }
		public double ElapsedHoursCapped { get; }
		public bool WasCapped { get; }
		public bool IsValid { get; }
		public string Reason { get; }

		public OfflineProgressResult(
			IReadOnlyDictionary<ResourceType, int> deltas,
			TimeSpan elapsed,
			double elapsedHoursCapped,
			bool wasCapped,
			bool isValid,
			string reason)
		{
			Deltas = deltas;
			Elapsed = elapsed;
			ElapsedHoursCapped = elapsedHoursCapped;
			WasCapped = wasCapped;
			IsValid = isValid;
			Reason = reason;
		}

		public static OfflineProgressResult Empty(string reason)
		{
			return new OfflineProgressResult(
				new Dictionary<ResourceType, int>(),
				TimeSpan.Zero,
				0,
				false,
				false,
				reason);
		}
	}
}