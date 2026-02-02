namespace Script.BuildingSystem.States
{
    /// <summary>
    /// State Pattern - Interface per stati BuildingPlacer.
    /// 
    /// DESIGN: Event-driven (NO Input polling in OnUpdate!)
    /// Stati ricevono COMANDI da BuildingPlacer, non leggono Input.GetKey direttamente.
    /// 
    /// Pattern corretto: Input handlers → BuildingPlacer public methods → State event handlers
    /// </summary>
    public interface IPlacementState
    {
        /// <summary>
        /// Chiamato quando si entra nello stato.
        /// Setup iniziale (es. attiva preview).
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Chiamato quando si esce dallo stato.
        /// Cleanup risorse (es. disattiva preview).
        /// </summary>
        void OnExit();

        /// <summary>
        /// Chiamato ogni frame da BuildingPlacer.Update() se lo stato lo richiede.
        /// Usa solo per aggiornamenti visivi (preview position), NON per input polling.
        /// </summary>
        void OnUpdate();

        // ========== EVENT HANDLERS (chiamati da BuildingPlacer public API) ==========

        /// <summary>
        /// Evento: Utente seleziona edificio da piazzare.
        /// Trigger: UI button click, keyboard shortcut, etc.
        /// </summary>
        void OnBuildingSelected(BuildingConfigSO config);

        /// <summary>
        /// Evento: Utente conferma piazzamento (es. Left Click).
        /// Trigger: Mouse click, UI button, etc.
        /// </summary>
        void OnPlacementConfirmed();

        /// <summary>
        /// Evento: Utente cancella piazzamento (es. Right Click, Escape).
        /// Trigger: Mouse right click, ESC key, UI cancel button.
        /// </summary>
        void OnPlacementCancelled();

        /// <summary>
        /// Nome stato per debugging/logging.
        /// </summary>
        string StateName { get; }
    }
}
