#if UNITY_EDITOR
using Script.ResourceSystem.Enums;
using UnityEngine;

namespace Script.Core.Events
{
    /// <summary>
    /// Esempio di utilizzo del GlobalEventBus.
    /// Dimostra pattern di subscribe/unsubscribe corretto e pubblicazione eventi.
    /// NOTA: Questo file è solo documentazione/esempio - non usare in produzione.
    /// </summary>
    public class GlobalEventBusUsageExample : MonoBehaviour
    {
        #region Example 1: Basic Subscribe/Unsubscribe

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<ResourceCollectedEvent>(HandleResourceCollected);
            GlobalEventBus.Subscribe<BuildingPlacedEvent>(HandleBuildingPlaced);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<ResourceCollectedEvent>(HandleResourceCollected);
            GlobalEventBus.Unsubscribe<BuildingPlacedEvent>(HandleBuildingPlaced);
        }

        #endregion

        #region Example 2: Event Handlers

        private void HandleResourceCollected(ResourceCollectedEvent evt)
        {
            Debug.Log($"[Example] Risorsa raccolta: {evt.Type} x{evt.Amount} alla posizione {evt.Position}");
            // Esempio: Aggiorna UI, riproduci suono, spawna VFX, etc.
        }

        private void HandleBuildingPlaced(BuildingPlacedEvent evt)
        {
            Debug.Log($"[Example] Edificio piazzato: {evt.BuildingName} alla posizione {evt.GridPosition}");
            // Esempio: Aggiorna minimap, riproduci animazione, sblocca achievement, etc.
        }

        #endregion

        #region Example 3: Publishing Events

        [ContextMenu("Test Publish Resource Event")]
        private void TestPublishResourceEvent()
        {
            // Crea evento (struct allocation - zero GC se su stack)
            var evt = new ResourceCollectedEvent(
                type: ResourceType.Wood,
                amount: 10,
                position: new Vector2Int(5, 5)
            );

            // Pubblica a tutti i subscriber
            GlobalEventBus.Publish(evt);
            
            Debug.Log("[Example] Evento ResourceCollected pubblicato");
        }

        [ContextMenu("Test Publish Building Event")]
        private void TestPublishBuildingEvent()
        {
            var evt = new BuildingPlacedEvent(
                building: null, // In produzione sarebbe l'istanza reale
                gridPosition: new Vector3Int(10, 10, 0),
                buildingName: "TestBuilding"
            );

            GlobalEventBus.Publish(evt);
            
            Debug.Log("[Example] Evento BuildingPlaced pubblicato");
        }

        #endregion

        #region Example 4: Multiple Handlers Same Event

        private void OnEnable_MultipleHandlers()
        {
            GlobalEventBus.Subscribe<ResourceCollectedEvent>(HandleResourceCollected);
            GlobalEventBus.Subscribe<ResourceCollectedEvent>(HandleResourceCollected_UI);
            GlobalEventBus.Subscribe<ResourceCollectedEvent>(HandleResourceCollected_Audio);
            // Tutti e tre verranno chiamati quando l'evento viene pubblicato!
        }

        private void HandleResourceCollected_UI(ResourceCollectedEvent evt)
        {
            // Logica specifica UI
        }

        private void HandleResourceCollected_Audio(ResourceCollectedEvent evt)
        {
            // Logica specifica Audio (play sound effect)
        }

        #endregion

        #region Example 5: Anti-Pattern (DA EVITARE)

        // ❌ SBAGLIATO: Subscribe senza Unsubscribe
        private void BadExample_NoUnsubscribe()
        {
            GlobalEventBus.Subscribe<ResourceCollectedEvent>(HandleResourceCollected);
            // ❌ PROBLEMA: Quando questo GameObject viene distrutto, l'handler rimane registrato
            // → MEMORY LEAK e possibili NullReferenceException
        }

        // ❌ SBAGLIATO: Creare dipendenze dirette negli handler
        private void BadExample_DirectDependency(ResourceCollectedEvent evt)
        {
            // ❌ PROBLEMA: Accoppiamento forte
            // var uiManager = FindObjectOfType<UIManager>();
            // uiManager.UpdateResourceDisplay(evt.Type, evt.Amount);
            
            // ✅ SOLUZIONE: UIManager dovrebbe subscribere a ResourceCollectedEvent
            // e gestire l'aggiornamento internamente
        }

        // ❌ SBAGLIATO: Logica pesante negli handler
        private void BadExample_HeavyLogic(ResourceCollectedEvent evt)
        {
            // ❌ PROBLEMA: Handler dovrebbero essere veloci
            // for (int i = 0; i < 1000000; i++) { /* heavy computation */ }
            
            // ✅ SOLUZIONE: Usa Coroutine o async/await per operazioni pesanti
        }

        #endregion

        #region Example 6: Best Practices

        /// <summary>
        /// BEST PRACTICE: Pattern completo con null check e error handling
        /// </summary>
        public class BestPracticeExample : MonoBehaviour
        {
            private bool _isSubscribed = false;

            private void OnEnable()
            {
                if (!_isSubscribed)
                {
                    GlobalEventBus.Subscribe<ResourceCollectedEvent>(OnResourceCollected);
                    _isSubscribed = true;
                }
            }

            private void OnDisable()
            {
                if (_isSubscribed)
                {
                    GlobalEventBus.Unsubscribe<ResourceCollectedEvent>(OnResourceCollected);
                    _isSubscribed = false;
                }
            }

            private void OnResourceCollected(ResourceCollectedEvent evt)
            {
                // Null check (se evento contiene oggetti Unity)
                if (evt.Position == default)
                {
                    Debug.LogWarning("Evento ricevuto con dati invalidi");
                    return;
                }

                // Logica veloce e sicura
                ProcessResourceCollection(evt);
            }

            private void ProcessResourceCollection(ResourceCollectedEvent evt)
            {
                // Implementazione...
            }
        }

        #endregion

        #region Example 7: Usage in Manager Classes

        /// <summary>
        /// ESEMPIO: Come un Manager dovrebbe pubblicare eventi
        /// </summary>
        public class ExampleManager : MonoBehaviour
        {
            public void CollectResource(ResourceType type, int amount, Vector2Int position)
            {
                // 1. Esegui logica business
                UpdateInventory(type, amount);

                // 2. Pubblica evento DOPO che la logica è completa
                var evt = new ResourceCollectedEvent(type, amount, position);
                GlobalEventBus.Publish(evt);

                // 3. Altri sistemi reagiscono all'evento automaticamente
                // (UI, Audio, VFX, etc.) senza che questo Manager li conosca!
            }

            private void UpdateInventory(ResourceType type, int amount)
            {
                // Implementazione logica business...
            }
        }

        #endregion
    }
}
#endif
