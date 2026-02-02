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
        private readonly BuildingPlacer _context;

        public string StateName => "Idle";

        public IdlePlacementState(BuildingPlacer context)
        {
            _context = context;
        }

        public void OnEnter()
        {
            #if UNITY_EDITOR
            Debug.Log("[PlacementFSM] → Idle (nessun edificio selezionato)");
            #endif
            
            // Assicurati che preview sia disattivata
            _context.ClearPreview();
        }

        public void OnUpdate()
        {
            // Idle: nessuna logica Update necessaria
            // Attende OnBuildingSelected da input esterno
        }

        public void OnExit()
        {
            // Nessun cleanup necessario
        }

        // ========== EVENT HANDLERS ==========

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
    }
}
