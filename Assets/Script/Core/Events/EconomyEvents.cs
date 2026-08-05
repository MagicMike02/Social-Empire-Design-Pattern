using Script.ResourceSystem.Enums;
using System.Collections.Generic;

namespace Script.Core.Events
{
	/// <summary>
	/// Pubblicato quando la quantità di una risorsa nell'economia cambia.
	/// Publisher: GameEconomyManager
	/// Subscribers: UI (resource display), Notifications, Achievements
	/// </summary>
	public readonly struct ResourceAmountChangedEvent
	{
		public readonly ResourceType Type;
		public readonly int CurrentAmount;
		public readonly int Delta;

		public ResourceAmountChangedEvent(ResourceType type, int currentAmount, int delta)
		{
			Type = type;
			CurrentAmount = currentAmount;
			Delta = delta;
		}
	}
}
