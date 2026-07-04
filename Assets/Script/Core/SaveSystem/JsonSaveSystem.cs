using System;
using System.IO;
using UnityEngine;

namespace Script.Core.SaveSystem
{
    public class JsonSaveSystem : IPersistenceManager
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        public void SaveGame(GameSaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Game saved to {SavePath}");
        }

        public GameSaveData LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning($"Save file not found at {SavePath}");
                return null;
            }

            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"Game loaded from {SavePath}");
            return data;
        }

        public void DeleteGame()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log($"Save file deleted at {SavePath}");
            }
            else
            {
                Debug.LogWarning($"Save file not found at {SavePath} for deletion");
            }
        }
    }
}