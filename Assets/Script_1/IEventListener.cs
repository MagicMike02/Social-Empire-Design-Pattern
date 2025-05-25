using Script.GridSystem;

namespace Script
{
    public interface IEventListener
    {
        // Dichiara il metodo per gestire un evento.
        void OnEvent(GridEvent eventInstance);
    }
}