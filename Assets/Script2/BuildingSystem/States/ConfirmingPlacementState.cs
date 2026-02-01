using UnityEngine;

namespace Script2.BuildingSystem.States
{
    /// <summary>
    /// Stato Confirming: Esegue placement effettivo via Command Pattern.
    /// 
    /// Transizioni:
    /// - OnEnter → Esegue placement → Transizione automatica a Idle
    /// 
    /// NOTE: Stato di transizione rapida (no OnUpdate, esecuzione immediata).
    /// </summary>
    public class ConfirmingPlacementState : IPlacementState
    {
        private readonly BuildingPlacer _context;
        private readonly BuildingConfigSO _selectedConfig;

        public string StateName => "Confirming";

        public ConfirmingPlacementState(BuildingPlacer context, BuildingConfigSO config)
        {
            _context = context;
            _selectedConfig = config;
        }

        public void OnEnter()
        {
            #if UNITY_EDITOR
            Debug.Log($"[PlacementFSM] → Confirming (executing placement for {_selectedConfig.name})");
            #endif
            
            bool success = false;
            
            try
            {
                // Esegui placement via Command Pattern
                success = _context.ExecutePlacementCommand();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfirmingPlacementState] Exception during placement: {ex.Message}\n{ex.StackTrace}");
                success = false;
            }
            finally
            {
                // ✅ SAFETY: SEMPRE esegui transizione (anche se exception)
                _context.TransitionTo(new IdlePlacementState(_context));
                
                if (!success)
                {
                    Debug.LogWarning("[ConfirmingPlacementState] Placement failed, returned to Idle");
                }
            }
        }

        public void OnUpdate()
        {
            // Nessuna logica: transizione immediata in OnEnter
        }

        public void OnExit()
        {
            // Disattiva preview (placement completato o fallito)
            _context.EnablePreviewMode(false);
            _context.ClearPreview();
        }

        // ========== EVENT HANDLERS ==========

        public void OnBuildingSelected(BuildingConfigSO config)
        {
            // Confirming: ignorato (transizione in corso)
            #if UNITY_EDITOR
            Debug.LogWarning("[ConfirmingPlacementState] OnBuildingSelected ignorato (già in transizione)");
            #endif
        }

        public void OnPlacementConfirmed()
        {
            // Confirming: già confermato, ignorato
        }

        public void OnPlacementCancelled()
        {
            // ✅ SAFETY: Permetti cancel anche in Confirming (force reset)
            Debug.LogWarning("[ConfirmingPlacementState] Force cancel during confirmation! Returning to Idle.");
            _context.TransitionTo(new IdlePlacementState(_context));
        }
    }
}
