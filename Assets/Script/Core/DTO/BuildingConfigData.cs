using System.Collections.Generic;

namespace Script.Core.DTO
{
	/// <summary>
	/// Configurazione immutabile di un edificio per validazione e trasporto.
	/// Record C# puro — nessuna dipendenza UnityEngine.
	/// </summary>
	/// <remarks>
	/// La chiave del dizionario è il nome dell'enum ResourceType.
	/// </remarks>
	public record BuildingConfigData
	{
		/// <summary>Identificativo univoco del tipo di edificio.</summary>
		public string Id { get; init; }

		public int Width { get; init; }
		public int Height { get; init; }

		/// <summary>Costi per tipo di risorsa (chiave = nome ResourceType, valore = quantità).</summary>
		public IReadOnlyDictionary<string, int> Costs { get; init; }

		/// <summary>Livello giocatore richiesto (0 = nessun requisito).</summary>
		public int RequiredLevel { get; init; }

		public BuildingConfigData(
			string id,
			int width,
			int height,
			IReadOnlyDictionary<string, int> costs = null,
			int requiredLevel = 0)
		{
			Id = id;
			Width = width;
			Height = height;
			Costs = costs ?? new Dictionary<string, int>();
			RequiredLevel = requiredLevel;
		}

		/// <summary>Factory di convenienza per edificio 1×1 con costo singolo.</summary>
		public static BuildingConfigData Single(string id, string resourceType, int cost) =>
			new(id, 1, 1, new Dictionary<string, int> { [resourceType] = cost });
	}
}
