using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script2.Core.Events
{
    /// <summary>
    /// GlobalEventBus: Event aggregator centralizzato per comunicazione inter-system.
    /// Pattern: Observer/Pub-Sub con tipo-safe events (struct-based).
    /// MEMORY SAFE: Tutti gli eventi devono essere unsubscribed in OnDestroy.
    /// </summary>
    public static class GlobalEventBus
    {
        #region Private Fields
        
        // Dictionary: Type → Delegate
        // Ogni tipo di evento (struct) ha la sua lista di handler
        private static readonly Dictionary<Type, Delegate> _eventHandlers = new();
        
        #endregion

        #region Subscribe/Unsubscribe

        /// <summary>
        /// Sottoscrivi un handler per un tipo di evento.
        /// IMPORTANTE: Devi chiamare Unsubscribe in OnDestroy per evitare memory leak!
        /// </summary>
        /// <typeparam name="T">Tipo evento (deve essere struct)</typeparam>
        /// <param name="handler">Callback da invocare quando l'evento viene pubblicato</param>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                Debug.LogWarning("[GlobalEventBus] Tentativo di subscribe con handler null");
                return;
            }

            var eventType = typeof(T);

            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = null;
            }

            _eventHandlers[eventType] = Delegate.Combine(_eventHandlers[eventType], handler);

            #if UNITY_EDITOR
            Debug.Log($"[GlobalEventBus] ✓ Subscribed to {eventType.Name} (total subscribers: {GetSubscriberCount<T>()})");
            #endif
        }

        /// <summary>
        /// Disiscrive un handler da un tipo di evento.
        /// CRITICO: Chiamare sempre in OnDestroy per prevenire memory leak!
        /// </summary>
        /// <typeparam name="T">Tipo evento (deve essere struct)</typeparam>
        /// <param name="handler">Callback da rimuovere</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                Debug.LogWarning("[GlobalEventBus] Tentativo di unsubscribe con handler null");
                return;
            }

            var eventType = typeof(T);

            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = Delegate.Remove(_eventHandlers[eventType], handler);

                // Cleanup se nessun subscriber rimasto
                if (_eventHandlers[eventType] == null)
                {
                    _eventHandlers.Remove(eventType);
                }

                #if UNITY_EDITOR
                Debug.Log($"[GlobalEventBus] ✓ Unsubscribed from {eventType.Name} (remaining: {GetSubscriberCount<T>()})");
                #endif
            }
            else
            {
                Debug.LogWarning($"[GlobalEventBus] Tentativo di unsubscribe da {eventType.Name} ma nessun handler registrato");
            }
        }

        #endregion

        #region Publish

        /// <summary>
        /// Pubblica un evento a tutti i subscriber.
        /// </summary>
        /// <typeparam name="T">Tipo evento (deve essere struct)</typeparam>
        /// <param name="eventData">Dati evento da passare ai subscriber</param>
        public static void Publish<T>(T eventData) where T : struct
        {
            var eventType = typeof(T);

            if (_eventHandlers.TryGetValue(eventType, out var handler))
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);

                    #if UNITY_EDITOR
                    Debug.Log($"[GlobalEventBus] → Published {eventType.Name} to {GetSubscriberCount<T>()} subscriber(s)");
                    #endif
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GlobalEventBus] Errore durante invocazione handler per {eventType.Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log($"[GlobalEventBus] → Published {eventType.Name} ma nessun subscriber (ignorato)");
                #endif
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Ottiene il numero di subscriber per un tipo di evento.
        /// Utile per debugging e monitoring.
        /// </summary>
        public static int GetSubscriberCount<T>() where T : struct
        {
            var eventType = typeof(T);
            if (_eventHandlers.TryGetValue(eventType, out var handler) && handler != null)
            {
                return handler.GetInvocationList().Length;
            }
            return 0;
        }

        /// <summary>
        /// Pulisce TUTTI gli eventi sottoscritti.
        /// ATTENZIONE: Usare solo per cleanup globale (es. cambio scena, reset completo).
        /// Non chiamare durante gameplay normale!
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            var eventCount = _eventHandlers.Count;
            _eventHandlers.Clear();

            Debug.LogWarning($"[GlobalEventBus] ⚠️ ClearAllSubscriptions: rimossi {eventCount} tipi di eventi");
        }

        /// <summary>
        /// Ottiene statistiche del bus per monitoring.
        /// </summary>
        public static string GetStats()
        {
            var totalEventTypes = _eventHandlers.Count;
            var totalSubscribers = 0;

            foreach (var handler in _eventHandlers.Values)
            {
                if (handler != null)
                {
                    totalSubscribers += handler.GetInvocationList().Length;
                }
            }

            return $"Event Types: {totalEventTypes}, Total Subscribers: {totalSubscribers}";
        }

        #endregion

        #region Editor Debugging

        #if UNITY_EDITOR
        /// <summary>
        /// Ottiene lista di tutti gli eventi registrati (solo editor).
        /// </summary>
        public static Dictionary<string, int> GetRegisteredEvents()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _eventHandlers)
            {
                var count = kvp.Value?.GetInvocationList().Length ?? 0;
                result[kvp.Key.Name] = count;
            }
            return result;
        }
        #endif

        #endregion
    }
}
