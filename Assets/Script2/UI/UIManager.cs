using UnityEngine;
using VContainer;
using Script2.ResourceSystem.ResourceUI;
using Script2.Core.Events;

namespace Script2.UI
{
    /// <summary>
    /// UI Manager centralizzato - Coordina tutti gli elementi UI del gioco.
    /// 
    /// RESPONSABILITÀ:
    /// - Riferimenti a tutti i panel UI (Resource Display, Zone Feedback, Building UI, etc.)
    /// - Gestione visibilità panel (Show/Hide)
    /// - Coordinamento transizioni UI
    /// - Single point of access per UI systems
    /// 
    /// DESIGN:
    /// - Dependency Injection (VContainer) per UI components
    /// - Non gestisce logica UI (delegata a componenti specifici)
    /// - Pattern: Facade per UI subsystems
    /// 
    /// FUTURE EXPANSION:
    /// - Building Selection UI Panel
    /// - Unit Selection UI Panel
    /// - Settings Panel
    /// - Pause Menu
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Dependencies (Injected by VContainer)
        
        private ResourceDisplayUI _resourceDisplayUI;

        [Inject]
        public void Construct(ResourceDisplayUI resourceDisplayUI)
        {
            _resourceDisplayUI = resourceDisplayUI;
        }
        
        #endregion

        #region Future Panels (Inspector - Keep SerializeField)
        
        [Header("Future Panels (Placeholder)")]
        [SerializeField] private GameObject _buildingSelectionPanel; // Future: UI per selezionare edifici
        [SerializeField] private GameObject _unitSelectionPanel;     // Future: UI per unità selezionate
        [SerializeField] private GameObject _settingsPanel;          // Future: Settings menu

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            InitializeUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromZoneEvents();
        }

        #endregion

        #region Initialization

        private void ValidateReferences()
        {
            if (_resourceDisplayUI == null)
            {
                Debug.LogError("[UIManager] ResourceDisplayUI non iniettato da VContainer!");
            }

            // Log warnings per future panels non ancora implementati
            if (_buildingSelectionPanel == null)
            {
                Debug.Log("[UIManager] Building Selection Panel non assegnato (future feature)");
            }

            if (_unitSelectionPanel == null)
            {
                Debug.Log("[UIManager] Unit Selection Panel non assegnato (future feature)");
            }

            if (_settingsPanel == null)
            {
                Debug.Log("[UIManager] Settings Panel non assegnato (future feature)");
            }
        }

        private void InitializeUI()
        {
            // Setup iniziale UI
            // Per ora i panel si auto-inizializzano (ResourceDisplayUI in Start, etc.)
            // Future: Centralizzare qui inizializzazione

            HideAllFuturePanels();
            SubscribeToZoneEvents();

            Debug.Log("[UIManager] ✓ UI Initialized");
        }

        private void HideAllFuturePanels()
        {
            // Nasconde panel future non ancora implementati
            if (_buildingSelectionPanel != null)
                _buildingSelectionPanel.SetActive(false);

            if (_unitSelectionPanel != null)
                _unitSelectionPanel.SetActive(false);

            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        private void SubscribeToZoneEvents()
        {
            GlobalEventBus.Subscribe<ZoneUnlockedEvent>(OnZoneUnlocked);
            GlobalEventBus.Subscribe<ZonePurchaseFailedEvent>(OnZonePurchaseFailed);
        }

        private void UnsubscribeFromZoneEvents()
        {
            GlobalEventBus.Unsubscribe<ZoneUnlockedEvent>(OnZoneUnlocked);
            GlobalEventBus.Unsubscribe<ZonePurchaseFailedEvent>(OnZonePurchaseFailed);
        }

        #endregion

        #region Zone Event Handlers

        private void OnZoneUnlocked(ZoneUnlockedEvent evt)
        {
            Debug.Log($"[UI] Zona sbloccata: {evt.ZonePosition}");
            // Future: Mostra animazione, suono, popup celebrativo
        }

        private void OnZonePurchaseFailed(ZonePurchaseFailedEvent evt)
        {
            Debug.LogWarning($"[UI] Acquisto zona fallito: {evt.ZoneCoord} - {evt.Reason}");
            // Future: Mostra popup errore, shake camera, suono errore
        }

        #endregion

        #region Public API (Future Expansion)

        /// <summary>
        /// Mostra building selection panel (future).
        /// Chiamato da: UI Button, Input handler
        /// </summary>
        public void ShowBuildingSelection()
        {
            if (_buildingSelectionPanel != null)
            {
                _buildingSelectionPanel.SetActive(true);
                Debug.Log("[UIManager] Building Selection Panel shown");
            }
            else
            {
                Debug.LogWarning("[UIManager] Cannot show Building Selection Panel: not assigned");
            }
        }

        /// <summary>
        /// Nasconde building selection panel.
        /// </summary>
        public void HideBuildingSelection()
        {
            if (_buildingSelectionPanel != null)
            {
                _buildingSelectionPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Mostra unit selection panel (future).
        /// </summary>
        public void ShowUnitSelection()
        {
            if (_unitSelectionPanel != null)
            {
                _unitSelectionPanel.SetActive(true);
                Debug.Log("[UIManager] Unit Selection Panel shown");
            }
        }

        /// <summary>
        /// Nasconde unit selection panel.
        /// </summary>
        public void HideUnitSelection()
        {
            if (_unitSelectionPanel != null)
            {
                _unitSelectionPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Toggle settings panel.
        /// </summary>
        public void ToggleSettings()
        {
            if (_settingsPanel != null)
            {
                bool isActive = _settingsPanel.activeSelf;
                _settingsPanel.SetActive(!isActive);
                Debug.Log($"[UIManager] Settings Panel {(isActive ? "hidden" : "shown")}");
            }
        }

        /// <summary>
        /// Nasconde tutti i panel opzionali (es. durante gameplay).
        /// </summary>
        public void HideAllOptionalPanels()
        {
            HideBuildingSelection();
            HideUnitSelection();

            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        #endregion

        #region Accessors (Read-Only)

        /// <summary>
        /// Accesso read-only a ResourceDisplayUI.
        /// </summary>
        public ResourceDisplayUI ResourceDisplay => _resourceDisplayUI;

        #endregion

        #region Debug Utilities

#if UNITY_EDITOR
        [ContextMenu("Debug: Show All Panels")]
        private void DebugShowAllPanels()
        {
            if (_buildingSelectionPanel != null) _buildingSelectionPanel.SetActive(true);
            if (_unitSelectionPanel != null) _unitSelectionPanel.SetActive(true);
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
            Debug.Log("[UIManager] DEBUG: All panels shown");
        }

        [ContextMenu("Debug: Hide All Panels")]
        private void DebugHideAllPanels()
        {
            HideAllOptionalPanels();
            Debug.Log("[UIManager] DEBUG: All panels hidden");
        }
#endif

        #endregion
    }
}
