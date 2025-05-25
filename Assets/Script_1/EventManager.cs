using System.Collections.Generic;
using Script.GridSystem;

namespace Script
{
    public class EventManager : IEventManager
    {
        // Utilizza un dizionario per memorizzare i listener per ciascun tipo di evento.
        private Dictionary<string, List<IEventListener>> listeners = new Dictionary<string, List<IEventListener>>();

        // Implementa il metodo per registrare un listener per un tipo di evento.
        public void RegisterListener(string eventType, IEventListener listener)
        {
            // Se il tipo di evento non è presente nel dizionario, aggiungilo.
            if (!listeners.ContainsKey(eventType))
            {
                listeners[eventType] = new List<IEventListener>();
            }
            // Aggiungi il listener all'elenco dei listener per quel tipo di evento.
            listeners[eventType].Add(listener);
        }

        // Implementa il metodo per rimuovere un listener per un tipo di evento.
        public void UnregisterListener(string eventType, IEventListener listener)
        {
            // Se il tipo di evento è presente nel dizionario, rimuovi il listener dall'elenco.
            if (listeners.ContainsKey(eventType))
            {
                listeners[eventType].Remove(listener);
            }
        }

        // Implementa il metodo per attivare un evento.
        public void RaiseEvent(GridEvent eventInstance)
        {
            // Ottieni il tipo dell'evento.
            string eventType = eventInstance.GetType().Name;
            // Se ci sono listener registrati per quel tipo di evento, notificli.
            if (listeners.ContainsKey(eventType))
            {
                // Crea una copia della lista per evitare la ConcurrentModificationException.
                List<IEventListener> eventListeners = new List<IEventListener>(listeners[eventType]);
                foreach (IEventListener listener in eventListeners)
                {
                    listener.OnEvent(eventInstance);
                }
            }
        }
    }
}