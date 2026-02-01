using UnityEngine;

namespace Script2.BuildingSystem.States
{
    /// <summary>
    /// Stato Previewing: Mostra preview edificio, segue mouse.
    /// 
    /// Transizioni:
    /// - OnPlacementConfirmed → ConfirmingPlacementState (se posizione valida)
    /// - OnPlacementCancelled → IdlePlacementState
    /// </summary>
    public class PreviewingPlacementState : IPlacementState
    {
        private readonly BuildingPlacer _context;
        private readonly BuildingConfigSO _selectedConfig;

        public string StateName => "Previewing";

        public PreviewingPlacementState(BuildingPlacer context, BuildingConfigSO config)
        {
            _context = context;
            _selectedConfig = config;
        }

        public void OnEnter()
        {
            #if UNITY_EDITOR
            Debug.Log($"[PlacementFSM] → Previewing ({_selectedConfig.name})");
            #endif

            // Attiva preview
            _context.SetSelectedConfig(_selectedConfig);
            _context.EnablePreviewMode(true);
        }

        public void OnUpdate()
        {
            // Update posizione preview (segue mouse)
            _context.UpdatePlacementPreviewInternal();
        }

        public void OnExit()
        {
            // Preview rimane attiva fino a stato Confirming
            // Verrà disattivata in Confirming.OnExit o Idle.OnEnter
        }

        // ========== EVENT HANDLERS ==========

        public void OnBuildingSelected(BuildingConfigSO config)
        {
            // Cambia edificio selezionato (resta in Previewing)
            if (config == null)
            {
                // Deselect → torna a Idle
                _context.TransitionTo(new IdlePlacementState(_context));
            }
            else
            {
                // Switch a nuovo edificio
                _context.TransitionTo(new PreviewingPlacementState(_context, config));
            }
        }

        public void OnPlacementConfirmed()
        {
            // Verifica se posizione valida
            if (!_context.CanPlaceAtCurrentPosition())
            {
                #if UNITY_EDITOR
                Debug.LogWarning("[PreviewingPlacementState] Cannot confirm: invalid position");
                #endif
                // Resta in Previewing (retry)
                return;
            }

            // Transizione a Confirming (esegue placement)
            _context.TransitionTo(new ConfirmingPlacementState(_context, _selectedConfig));
        }

        public void OnPlacementCancelled()
        {
            // Cancella placement → torna a Idle
            _context.TransitionTo(new IdlePlacementState(_context));
        }
    }
}
