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
            if (data == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[JsonSaveSystem] SaveGame chiamato con data null. Salvataggio saltato.");
#endif
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
                Debug.Log($"Game saved to {SavePath}");
#endif
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
#if UNITY_EDITOR
                Debug.LogError($"[JsonSaveSystem] Errore I/O durante SaveGame: {ex.Message}");
#endif
            }
        }

        public GameSaveData LoadGame()
        {
            try
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
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
#if UNITY_EDITOR
                Debug.LogError($"[JsonSaveSystem] Errore I/O durante LoadGame: {ex.Message}. Fallback a stato default.");
#endif
                return null;
            }
        }

        public void DeleteGame()
        {
            try
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
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
#if UNITY_EDITOR
                Debug.LogError($"[JsonSaveSystem] Errore I/O durante DeleteGame: {ex.Message}");
#endif
            }
        }
    }
}