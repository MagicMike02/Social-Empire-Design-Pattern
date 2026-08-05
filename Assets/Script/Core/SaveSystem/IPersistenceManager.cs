using System;

namespace Script.Core.SaveSystem
{
    public interface IPersistenceManager
    {
        void SaveGame(GameSaveData data);
        GameSaveData LoadGame();
        void DeleteGame();
    }
}