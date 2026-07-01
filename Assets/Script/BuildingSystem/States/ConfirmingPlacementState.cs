using UnityEngine;

namespace Script.BuildingSystem.States
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
        #region Fields & Properties
        
        private readonly BuildingPlacer _context;
        private readonly BuildingConfigSO _selectedConfig;

        public string StateName => "Confirming";
        
        #endregion

        #region Initialization

        public ConfirmingPlacementState(BuildingPlacer context, BuildingConfigSO config)
        {
            _context = context;
            _selectedConfig = config;
        }
        
        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Entrata nello stato: tenta il piazzamento e torna subito in Idle.
        /// </summary>
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
#if UNITY_EDITOR
                Debug.LogError($"[ConfirmingPlacementState] Exception during placement: {ex.Message}\n{ex.StackTrace}");
#endif
                success = false;
            }
            finally
            {
                // SEMPRE esegui transizione (anche se exception)
                _context.TransitionTo(new IdlePlacementState(_context));
                
                if (!success)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[ConfirmingPlacementState] Placement failed, returned to Idle");
#endif
                }
            }
        }

        /// <summary>
        /// Aggiornamento frame-per-frame (non usato in Confirming).
        /// </summary>
        public void OnUpdate()
        {
            // Nessuna logica: transizione immediata in OnEnter
        }

        /// <summary>
        /// Uscita dallo stato: pulisce la preview.
        /// </summary>
        public void OnExit()
        {
            // Disattiva preview (placement completato o fallito)
            _context.EnablePreviewMode(false);
            _context.ClearPreview();
        }
        
        #endregion

        #region Event Handlers

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
            // Permetti cancel anche in Confirming (force reset)
#if UNITY_EDITOR
            Debug.LogWarning("[ConfirmingPlacementState] Force cancel during confirmation! Returning to Idle.");
#endif
            _context.TransitionTo(new IdlePlacementState(_context));
        }
        
        #endregion
    }
}
