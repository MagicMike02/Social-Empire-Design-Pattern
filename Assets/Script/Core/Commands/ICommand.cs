using System;
using System.Threading.Tasks;

namespace Script.Core.Commands
{
    /// <summary>
    /// Stato del comando nel suo lifecycle.
    /// Pending: Creato ma non ancora eseguito.
    /// Confirmed: Eseguito con successo (o redo).
    /// RolledBack: Annullato (undo) o fallito dopo un tentativo.
    /// </summary>
    public enum CommandState
    {
        Pending,
        Confirmed,
        RolledBack
    }

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
        /// Stato corrente del comando nel lifecycle.
        /// Viene aggiornato automaticamente da CommandHistory dopo Execute/Undo/Redo.
        /// </summary>
        CommandState State { get; set; }

        /// <summary>
        /// Esegue il comando (es. Piazza edificio, raccogli risorsa, muovi unità).
        /// </summary>
        /// <returns>
        /// True se eseguito con successo, False se fallito.
        /// Fallimenti possibili: validazione fallita, risorse insufficienti, celle occupate.
        /// </returns>
        bool Execute();

        /// <summary>
        /// Esegue il comando in modalità asincrona con optimistic update + conferma.
        /// Per ora esegue l'optimistic update e conferma immediatamente.
        /// In futuro: attenderà la conferma da IBackendService.
        /// </summary>
        /// <returns>
        /// True se eseguito e confermato con successo, False se fallito.
        /// </returns>
        Task<bool> ExecuteAsync();

        /// <summary>
        /// Conferma il comando dopo ricezione conferma server.
        /// Chiamato da CommandHistory quando il backend conferma l'operazione.
        /// Sincronizza valori autoritativi dal server (es. saldo oro).
        /// </summary>
        void Confirm();

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
