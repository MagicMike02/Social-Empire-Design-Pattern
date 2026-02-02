namespace Script.Core.Commands
{
    /// <summary>
    /// Command Pattern - Interface base universale per tutti i comandi del gioco.
    /// Utilizzato da: BuildingCommands, ResourceCommands, UnitCommands (future).
    /// Permette Undo/Redo tramite CommandHistory centralizzato.
    /// 
    /// DESIGN: Generico (non legato a specifici sistemi) per massima riusabilità.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Esegue il comando (es. Piazza edificio, raccogli risorsa, muovi unità).
        /// </summary>
        /// <returns>
        /// True se eseguito con successo, False se fallito.
        /// Fallimenti possibili: validazione fallita, risorse insufficienti, celle occupate.
        /// </returns>
        bool Execute();

        /// <summary>
        /// Annulla il comando (ripristina stato precedente).
        /// Es: Undo PlaceBuilding → Rimuove edificio + Refund 100% risorse
        /// Es: Undo CollectResource → Rispawna risorsa + Rimuove da inventory
        /// </summary>
        /// <returns>
        /// True se annullato con successo, False se impossibile annullare.
        /// </returns>
        bool Undo();

        /// <summary>
        /// Descrizione human-readable del comando (per UI history, debugging).
        /// Es: "Place Farm at (5, 10)", "Collect Wood at (3, 7)"
        /// </summary>
        string Description { get; }
    }
}
