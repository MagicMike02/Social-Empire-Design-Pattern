using Script.BuildingSystem;
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.Core.Events
{
    #region Resource System Events
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

    #endregion

    #region Economy System Events

    /// <summary>
    /// Pubblicato quando la quantità di una risorsa nell'economia cambia.
    /// Publisher: GameEconomyManager
    /// Subscribers: UI (resource display), Notifications, Achievements
    /// </summary>
    public readonly struct ResourceAmountChangedEvent
    {
        public readonly ResourceType Type;
        public readonly int CurrentAmount;
        public readonly int Delta; // Positivo = aggiunta, Negativo = spesa

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
        // Nota: IReadOnlyDictionary non è struct, ma evento è readonly quindi safe
        public readonly System.Collections.Generic.IReadOnlyDictionary<ResourceType, int> NewBalances;

        public ResourcesBatchChangedEvent(System.Collections.Generic.IReadOnlyDictionary<ResourceType, int> newBalances)
        {
            NewBalances = newBalances;
        }
    }

    #endregion

    #region Building System Events

    /// <summary>
    /// Pubblicato quando un edificio viene piazzato con successo sulla griglia.
    /// Publisher: BuildingPlacer / BuildingManager
    /// Subscribers: Grid (occupa celle), Audio, VFX, Achievements, Quests
    /// </summary>
    public readonly struct BuildingPlacedEvent
    {
        public readonly Building BuildingInstance;
        public readonly Vector3Int GridPosition;
        public readonly string BuildingName;

        public BuildingPlacedEvent(Building building, Vector3Int gridPosition, string buildingName)
        {
            BuildingInstance = building;
            GridPosition = gridPosition;
            BuildingName = buildingName;
        }
    }

    /// <summary>
    /// Pubblicato quando un edificio viene distrutto/rimosso.
    /// Publisher: BuildingManager
    /// Subscribers: Grid (libera celle), Resources (refund), UI, VFX
    /// </summary>
    public readonly struct BuildingDestroyedEvent
    {
        public readonly Building BuildingInstance;
        public readonly Vector3Int GridPosition;
        public readonly bool WasRefunded; // True se risorse restituite

        public BuildingDestroyedEvent(Building building, Vector3Int gridPosition, bool wasRefunded)
        {
            BuildingInstance = building;
            GridPosition = gridPosition;
            WasRefunded = wasRefunded;
        }
    }

    /// <summary>
    /// Pubblicato quando un edificio viene selezionato dall'utente.
    /// Publisher: InputManager / BuildingPlacer
    /// Subscribers: UI (info panel), Camera (focus), Selection highlight
    /// </summary>
    public readonly struct BuildingSelectedEvent
    {
        public readonly Building BuildingInstance;

        public BuildingSelectedEvent(Building building)
        {
            BuildingInstance = building;
        }
    }

    #endregion

    #region Grid System Events

    /// <summary>
    /// Pubblicato quando una o più celle della griglia vengono occupate.
    /// Publisher: GridManager
    /// Subscribers: Pathfinding (invalida cache), Minimap, FogOfWar
    /// </summary>
    public readonly struct CellsOccupiedEvent
    {
        public readonly Vector3Int OriginCell;
        public readonly int Width;
        public readonly int Height;
        public readonly GameObject Occupant; // Building, Unit, etc.

        public CellsOccupiedEvent(Vector3Int originCell, int width, int height, GameObject occupant)
        {
            OriginCell = originCell;
            Width = width;
            Height = height;
            Occupant = occupant;
        }
    }

    /// <summary>
    /// Pubblicato quando celle della griglia vengono liberate.
    /// Publisher: GridManager
    /// Subscribers: Pathfinding (invalida cache), BuildingPlacer (aggiorna validità preview)
    /// </summary>
    public readonly struct CellsFreedEvent
    {
        public readonly Vector3Int OriginCell;
        public readonly int Width;
        public readonly int Height;

        public CellsFreedEvent(Vector3Int originCell, int width, int height)
        {
            OriginCell = originCell;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Pubblicato quando una zona viene sbloccata (acquistata dal giocatore).
    /// Publisher: ZoneManager
    /// Subscribers: UI (aggiorna mappa), Audio, Tutorial
    /// </summary>
    public readonly struct ZoneUnlockedEvent
    {
        public readonly int ZoneIndex;
        public readonly Vector2Int ZonePosition;

        public ZoneUnlockedEvent(int zoneIndex, Vector2Int zonePosition)
        {
            ZoneIndex = zoneIndex;
            ZonePosition = zonePosition;
        }
    }

    /// <summary>
    /// Pubblicato quando il tentativo di acquisto zona fallisce.
    /// Publisher: ZoneManager
    /// Subscribers: ZoneFeedbackUI (mostra errore), AudioManager (suono errore)
    /// </summary>
    public readonly struct ZonePurchaseFailedEvent
    {
        public readonly Vector2Int ZoneCoord;
        public readonly string Reason; // "Insufficient Resources", "Already Unlocked", etc.

        public ZonePurchaseFailedEvent(Vector2Int zoneCoord, string reason)
        {
            ZoneCoord = zoneCoord;
            Reason = reason;
        }
    }

    #endregion

    #region Input System Events

    /// <summary>
    /// Pubblicato quando l'utente clicca su un tile della griglia.
    /// Publisher: InputManager
    /// Subscribers: BuildingPlacer (placement), UnitCommands (move to), Selection
    /// </summary>
    public readonly struct TileClickedEvent
    {
        public readonly Vector2Int GridPosition;
        public readonly Vector3 WorldPosition;

        public TileClickedEvent(Vector2Int gridPosition, Vector3 worldPosition)
        {
            GridPosition = gridPosition;
            WorldPosition = worldPosition;
        }
    }

    #endregion

    #region Pathfinding System Events (Future)

    /// <summary>
    /// Pubblicato quando un percorso viene trovato con successo.
    /// Publisher: PathfindingManager
    /// Subscribers: Debug visualization, Performance monitoring
    /// </summary>
    public readonly struct PathFoundEvent
    {
        public readonly Vector2Int Start;
        public readonly Vector2Int Goal;
        public readonly int PathLength;
        public readonly float ComputationTimeMs;

        public PathFoundEvent(Vector2Int start, Vector2Int goal, int pathLength, float computationTimeMs)
        {
            Start = start;
            Goal = goal;
            PathLength = pathLength;
            ComputationTimeMs = computationTimeMs;
        }
    }

    #endregion

    #region Game State Events (Future)

    /// <summary>
    /// Pubblicato quando il gioco viene salvato.
    /// Publisher: SaveManager
    /// Subscribers: UI (notification), Cloud sync (future)
    /// </summary>
    public readonly struct GameSavedEvent
    {
        public readonly string SaveFilePath;
        public readonly long Timestamp;

        public GameSavedEvent(string saveFilePath, long timestamp)
        {
            SaveFilePath = saveFilePath;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Pubblicato quando il gioco viene caricato.
    /// Publisher: SaveManager
    /// Subscribers: Tutti i Manager (reset state), UI, Camera
    /// </summary>
    public readonly struct GameLoadedEvent
    {
        public readonly long SaveTimestamp;

        public GameLoadedEvent(long saveTimestamp)
        {
            SaveTimestamp = saveTimestamp;
        }
    }

    #endregion
}
