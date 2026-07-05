using System.Collections.Generic;

namespace Script.Core.DTO
{
	/// <summary>
	/// Snapshot immutabile dello stato economico del giocatore.
	/// Record C# puro — nessuna dipendenza UnityEngine — utilizzabile
	/// sia lato client (optimistic update) sia lato server (validazione).
	/// </summary>
	/// <remarks>
	/// Mappa <see cref="Script.EconomySystem.GameEconomyManager.GetResourcesSnapshot"/>
	/// in un tipo serializzabile e trasportabile.
	/// </remarks>
	public record PlayerEconomyData
	{
		/// <summary>Saldo per tipo di risorsa (chiave = nome enum ResourceType).</summary>
		public IReadOnlyDictionary<string, int> Resources { get; init; }

		/// <summary>Livello del giocatore (riservato per validazioni future).</summary>
		public int Level { get; init; }

		public PlayerEconomyData(IReadOnlyDictionary<string, int> resources, int level = 0)
		{
			Resources = resources ?? new Dictionary<string, int>();
			Level = level;
		}

		/// <summary>Factory di convenienza per un singolo tipo di risorsa (es. test).</summary>
		public static PlayerEconomyData Of(string resourceType, int amount) =>
			new(new Dictionary<string, int> { [resourceType] = amount });
	}
}
