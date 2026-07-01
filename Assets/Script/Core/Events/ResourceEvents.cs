using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.Core.Events
{
    /// <summary>
    /// Pubblicato quando una risorsa viene raccolta dal giocatore.
    /// Publisher: ResourceManager
    /// Subscribers: UI (aggiornamento display), Economy (statistiche)
    /// </summary>
    public readonly struct ResourceCollectedEvent
    {
        public readonly ResourceType Type;
        public readonly int Amount;
        public readonly Vector2Int Position;

        public ResourceCollectedEvent(ResourceType type, int amount, Vector2Int position)
        {
            Type = type;
            Amount = amount;
            Position = position;
        }
    }

    /// <summary>
    /// Pubblicato quando una risorsa viene generata/spawned sulla griglia.
    /// Publisher: ResourceManager
    /// Subscribers: Minimap, FogOfWar (future)
    /// </summary>
    public readonly struct ResourceGeneratedEvent
    {
        public readonly ResourceType Type;
        public readonly Vector2Int Position;

        public ResourceGeneratedEvent(ResourceType type, Vector2Int position)
        {
            Type = type;
            Position = position;
        }
    }

    /// <summary>
    /// Pubblicato quando una risorsa inizia il processo di rigenerazione.
    /// Publisher: ResourceManager
    /// Subscribers: UI (timer regen), VFX (particle systems)
    /// </summary>
    public readonly struct ResourceRegenerationStartedEvent
    {
        public readonly Vector2Int Position;
        public readonly float RegenerationTime;

        public ResourceRegenerationStartedEvent(Vector2Int position, float regenerationTime)
        {
            Position = position;
            RegenerationTime = regenerationTime;
        }
    }

    /// <summary>
    /// Pubblicato quando una risorsa completa la rigenerazione.
    /// Publisher: ResourceManager
    /// Subscribers: Audio (sound effect), VFX
    /// </summary>
    public readonly struct ResourceRegeneratedEvent
    {
        public readonly Vector2Int Position;
        public readonly ResourceType Type;

        public ResourceRegeneratedEvent(Vector2Int position, ResourceType type)
        {
            Position = position;
            Type = type;
        }
    }
}
