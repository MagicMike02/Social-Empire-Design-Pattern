using Script.GridSystem;

namespace Script
{
    // Definiesce l'interfaccia IEventManager per la gestione degli eventi.
    public interface IEventManager
    {
        // Dichiara il metodo per registrare un listener per un tipo di evento.
        void RegisterListener(string eventType, IEventListener listener);

        // Dichiara il metodo per rimuovere un listener per un tipo di evento.
        void UnregisterListener(string eventType, IEventListener listener);

        // Dichiara il metodo per attivare un evento.
        void RaiseEvent(GridEvent eventInstance);
    }
}