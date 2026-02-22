using UnityEngine;

namespace Script.BuildingSystem.States
{
    /// <summary>
    /// Stato Idle: Nessun edificio selezionato, attesa input.
    /// 
    /// Transizioni:
    /// - OnBuildingSelected → PreviewingPlacementState
    /// </summary>
    public class IdlePlacementState : IPlacementState
    {
        #region Fields & Properties
        
        private readonly BuildingPlacer _context;

        public string StateName => "Idle";
        
        #endregion

        #region Initialization

        public IdlePlacementState(BuildingPlacer context)
        {
            _context = context;
        }
        
        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Inizializza lo stato Idle pulendo la preview.
        /// </summary>
        public void OnEnter()
        {
            #if UNITY_EDITOR
            Debug.Log("[PlacementFSM] → Idle (nessun edificio selezionato)");
            #endif
            
            // Assicurati che preview sia disattivata
            _context.ClearPreview();
        }

        /// <summary>
        /// Aggiornamento frame-per-frame (non necessario in Idle).
        /// </summary>
        public void OnUpdate()
        {
            // Idle: nessuna logica Update necessaria
            // Attende OnBuildingSelected da input esterno
        }

        /// <summary>
        /// Cleanup all'uscita dallo stato.
        /// </summary>
        public void OnExit()
        {
            // Nessun cleanup necessario
        }
        
        #endregion

        #region Event Handlers

        public void OnBuildingSelected(BuildingConfigSO config)
        {
            if (config == null)
            {
                Debug.LogWarning("[IdlePlacementState] OnBuildingSelected chiamato con config null");
                return;
            }

            // Transizione a Previewing
            _context.TransitionTo(new PreviewingPlacementState(_context, config));
        }

        public void OnPlacementConfirmed()
        {
            // Idle: nessun edificio selezionato, ignora
            #if UNITY_EDITOR
            Debug.LogWarning("[IdlePlacementState] OnPlacementConfirmed chiamato in Idle (ignorato)");
            #endif
        }

        public void OnPlacementCancelled()
        {
            // Force reset FSM (se chiamato da qualsiasi stato, torna sempre a Idle)
            // Utile se FSM in stato inconsistente
            _context.TransitionTo(new IdlePlacementState(_context));
        }
        
        #endregion
    }
}
