using System.Collections.Generic;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.Core.Config
{
	/// <summary>
	/// Configurazione globale per i parametri di avvio del gioco.
	/// Zero logica — solo dati serializzati.
	/// </summary>
	[CreateAssetMenu(menuName = "Social Empire/Config/Game Config", fileName = "GameConfig")]
	public class GameConfigSO : ScriptableObject
	{
		[Header("Nuova Partita — Risorse iniziali")]
		public List<ResourceStartAmount> startingResources = new();

		[Header("Villaggio")]
		public string defaultVillageName = "My Village";
		public int startingPlayerLevel = 1;

		[Header("Popolazione (placeholder)")]
		public int defaultMaxPopulation = 10;

		[System.Serializable]
		public struct ResourceStartAmount
		{
			public ResourceType type;
			public int amount;
		}
	}
}