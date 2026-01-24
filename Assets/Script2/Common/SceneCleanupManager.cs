using UnityEngine;
using UnityEngine.SceneManagement;
using Script2.BuildingSystem;

namespace Script2.Common
{
    /// <summary>
    /// Singleton Manager per la pulizia degli eventi statici tra le scene.
    /// Previene memory leaks causati da event handlers persistenti.
    /// 
    /// SETUP (Una volta sola):
    /// 1. Create GameObject in prima scena: "[Manager] EventCleanup"
    /// 2. Add EventCleanupManager component
    /// 3. DONE! Funziona automaticamente in tutte le altre scene
    /// 
    /// Non devi più pensarci - persiste tra le scene e si occupa da solo.
    /// </summary>
    public class EventCleanupManager : MonoBehaviour
    {
        public static EventCleanupManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern: una sola istanza
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Persiste tra le scene - non viene mai distrutto
            DontDestroyOnLoad(gameObject);

            // Sottoscrivi agli eventi di scena
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            #if UNITY_EDITOR
            Debug.Log("[EventCleanupManager] Initialized. Will cleanup events on scene transitions.");
            #endif
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // Quando scarichi una scena, pulisci gli handler
            CleanupAllEvents();

            #if UNITY_EDITOR
            Debug.Log($"[EventCleanupManager] Scene '{scene.name}' unloaded. Events cleared.");
            #endif
        }

        /// <summary>
        /// Pulisce tutti gli event handler statici per prevenire memory leaks.
        /// Chiamato automaticamente ad ogni transizione di scena.
        /// </summary>
        private void CleanupAllEvents()
        {
            BuildingEvents.ClearAllEvents();
            // Aggiungi qui altre pulizie se necessario
        }

        private void OnDestroy()
        {
            // Disiscrivi quando viene distrutto (cambio scena con DontDestroyOnLoad = false)
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

