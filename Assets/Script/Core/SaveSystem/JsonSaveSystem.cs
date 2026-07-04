using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Script.Core.SaveSystem
{
    public class JsonSaveSystem : IPersistenceManager
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        public void SaveGame(GameSaveData data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
            Debug.Log($"Game saved to {SavePath}");
#endif
        }

        public GameSaveData LoadGame()
        {
            if (!File.Exists(SavePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Save file not found at {SavePath}");
#endif
                return null;
            }

            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json);
#if UNITY_EDITOR
            Debug.Log($"Game loaded from {SavePath}");
#endif
            return data;
        }

        public void DeleteGame()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
#if UNITY_EDITOR
                Debug.Log($"Save file deleted at {SavePath}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Save file not found at {SavePath} for deletion");
#endif
            }
        }
    }
}