namespace Script.Core.Events
{
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
}
