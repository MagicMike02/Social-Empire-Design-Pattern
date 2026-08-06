using System.Collections.Generic;
using Script.Core.Config;
using Script.EconomySystem;
using Script.ResourceSystem.Enums;
using Script.UI.Toolkit.Data;
using VContainer;

namespace Script.UI.Toolkit.Data
{
	/// <summary>
	/// Implementazione concreta di IPlayerDataProvider.
	/// Classe C# pura — NON MonoBehaviour.
	/// Registrata in VContainer come Singleton.
	/// </summary>
	public sealed class PlayerDataProvider : IPlayerDataProvider
	{
		private readonly GameEconomyManager _economyManager;
		private readonly GameConfigSO _gameConfig;

		[Inject]
		public PlayerDataProvider(GameEconomyManager economyManager, GameConfigSO gameConfig)
		{
			_economyManager = economyManager;
			_gameConfig = gameConfig;
		}

		// Risorse
		public IReadOnlyDictionary<ResourceType, int> Resources => _economyManager.GetResourcesSnapshot();

		public int GetResourceAmount(ResourceType type) => _economyManager.GetResourceAmount(type);

		// Popolazione (placeholder — valori reali in futuro)
		public int CurrentPopulation => 0; // TODO: PopulationManager

		public int MaxPopulation => _gameConfig?.defaultMaxPopulation ?? 10;

		// Villaggio (placeholder — da GameConfigSO per ora)
		public string VillageName => _gameConfig?.defaultVillageName ?? "My Village";

		// Livello giocatore
		public int PlayerLevel => _gameConfig?.startingPlayerLevel ?? 1; // TODO: quando esiste PlayerProgressionManager, legge da lì
	}
}