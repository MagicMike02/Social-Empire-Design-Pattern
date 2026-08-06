using System.Collections.Generic;
using Script.ResourceSystem.Enums;

namespace Script.UI.Toolkit.Data
{
	/// <summary>
	/// Provider di dati del giocatore per UI Toolkit.
	/// Interfaccia C# pura — zero dipendenze UnityEngine.
	/// </summary>
	public interface IPlayerDataProvider
	{
		// Risorse
		IReadOnlyDictionary<ResourceType, int> Resources { get; }
		int GetResourceAmount(ResourceType type);

		// Popolazione (placeholder — valori reali in futuro)
		int CurrentPopulation { get; }
		int MaxPopulation { get; }

		// Villaggio (placeholder — da GameConfigSO per ora)
		string VillageName { get; }

		// Livello giocatore
		int PlayerLevel { get; }
	}
}