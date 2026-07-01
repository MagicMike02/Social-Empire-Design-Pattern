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

    /// <summary>
    /// Pubblicato quando multiple risorse cambiano simultaneamente (es. acquisto edificio).
    /// Publisher: GameEconomyManager
    /// Subscribers: UI (batch update), Transaction log
    /// </summary>
    public readonly struct ResourcesBatchChangedEvent
    {
        public readonly IReadOnlyDictionary<ResourceType, int> NewBalances;

        public ResourcesBatchChangedEvent(IReadOnlyDictionary<ResourceType, int> newBalances)
        {
            NewBalances = newBalances;
        }
    }
}
