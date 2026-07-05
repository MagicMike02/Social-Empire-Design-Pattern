using Script.Core.Events;
using Script.Core.SaveSystem;
using Script.ResourceSystem.ResourceUI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.UI
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
		private SaveManager _saveManager;

		[Inject]
		public void Construct(ResourceDisplayUI resourceDisplayUI, SaveManager saveManager)
		{
			try
			{
				_resourceDisplayUI = resourceDisplayUI;
				_saveManager = saveManager;
			}
			catch (System.Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[UIManager] Errore durante Construct: {ex.Message}");
#endif
			}
		}

		#endregion

		#region Future Panels (Inspector - Keep SerializeField)

		[Header("Future Panels (Placeholder)")]
		[SerializeField] private GameObject _buildingSelectionPanel; // Future: UI per selezionare edifici
		[SerializeField] private GameObject _unitSelectionPanel;     // Future: UI per unità selezionate
		[SerializeField] private GameObject _settingsPanel;          // Future: Settings menu (include Save/Load buttons)

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			if (!ValidateReferences())
			{
				return;
			}
		}

		private void Start()
		{
			if (!ValidateReferences())
			{
				return;
			}

			InitializeUI();
		}

		private void OnEnable()
		{
			SubscribeToZoneEvents();
		}

		private void OnDisable()
		{
			UnsubscribeFromZoneEvents();
		}

		#endregion

		#region Initialization

		/// <summary>
		/// Controlla la sanità dei collegamenti iniettati o serializzati (Future Panels inclusi).
		/// </summary>
		private bool ValidateReferences()
		{
			if (_resourceDisplayUI == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[UIManager] ResourceDisplayUI non iniettato da VContainer!");
#endif
				return false;
			}

			if (_saveManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[UIManager] SaveManager non iniettato da VContainer!");
#endif
				return false;
			}

			// Log warnings per future panels non ancora implementati
			if (_buildingSelectionPanel == null)
			{
#if UNITY_EDITOR
				Debug.Log("[UIManager] Building Selection Panel non assegnato (future feature)");
#endif
			}

			if (_unitSelectionPanel == null)
			{
#if UNITY_EDITOR
				Debug.Log("[UIManager] Unit Selection Panel non assegnato (future feature)");
#endif
			}

			if (_settingsPanel == null)
			{
#if UNITY_EDITOR
				Debug.Log("[UIManager] Settings Panel non assegnato (future feature)");
#endif
			}

			return true;
		}

		/// <summary>
		/// Imposta lo stato root del canvas nascondendo UI work-in-progress.
		/// </summary>
		private void InitializeUI()
		{
			// Setup iniziale UI
			// Per ora i panel si auto-inizializzano (ResourceDisplayUI in Start, etc.)
			// Future: Centralizzare qui inizializzazione

			HideAllFuturePanels();

			// Wire up Save/Load buttons if Settings Panel is assigned (S3-08)
			if (_settingsPanel != null)
			{
				// Find Save and Load buttons in the Settings Panel
				var saveButton = _settingsPanel.transform.Find("SaveButton");
				var loadButton = _settingsPanel.transform.Find("LoadButton");

				if (saveButton != null)
				{
					var buttonComponent = saveButton.GetComponent<UnityEngine.UI.Button>();
					if (buttonComponent != null)
					{
						buttonComponent.onClick.AddListener(OnSaveButtonClicked);
#if UNITY_EDITOR
						Debug.Log("[UIManager] Save button wired to OnSaveButtonClicked");
#endif
					}
				}

				if (loadButton != null)
				{
					var buttonComponent = loadButton.GetComponent<UnityEngine.UI.Button>();
					if (buttonComponent != null)
					{
						buttonComponent.onClick.AddListener(OnLoadButtonClicked);
#if UNITY_EDITOR
						Debug.Log("[UIManager] Load button wired to OnLoadButtonClicked");
#endif
					}
				}
			}

#if UNITY_EDITOR
			Debug.Log("[UIManager] ✓ UI Initialized");
#endif
		}

		/// <summary>
		/// Assicura l'invisibilità alla prima iterazione per componenti visuali placeholder.
		/// </summary>
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

		/// <summary>
		/// Iscrive UIManager come subscriber degli eventi relativi allo sblocco o fail di zones.
		/// </summary>
		private void SubscribeToZoneEvents()
		{
			GlobalEventBus.Subscribe<ZoneUnlockedEvent>(OnZoneUnlocked);
			GlobalEventBus.Subscribe<ZonePurchaseFailedEvent>(OnZonePurchaseFailed);
		}

		/// <summary>
		/// Rilascia memoria scollegando l'istanza core dalla matrice eventi GlobalEventBus.
		/// </summary>
		private void UnsubscribeFromZoneEvents()
		{
			GlobalEventBus.Unsubscribe<ZoneUnlockedEvent>(OnZoneUnlocked);
			GlobalEventBus.Unsubscribe<ZonePurchaseFailedEvent>(OnZonePurchaseFailed);
		}

		#endregion

		#region Zone Event Handlers

		/// <summary>
		/// Animazione e trigger UI su acquisto o sblocco zona di successo.
		/// </summary>
		private void OnZoneUnlocked(ZoneUnlockedEvent evt)
		{
#if UNITY_EDITOR
			Debug.Log($"[UI] Zona sbloccata: {evt.ZonePosition}");
#endif
			// Future: Mostra animazione, suono, popup celebrativo
		}

		/// <summary>
		/// Informative trigger interattivo (pop-up, sfx) quando l'utente manca di risorse per area.
		/// </summary>
		private void OnZonePurchaseFailed(ZonePurchaseFailedEvent evt)
		{
#if UNITY_EDITOR
			Debug.LogWarning($"[UI] Acquisto zona fallito: {evt.ZoneCoord} - {evt.Reason}");
#endif
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
#if UNITY_EDITOR
				Debug.Log("[UIManager] Building Selection Panel shown");
#endif
			}
			else
			{
#if UNITY_EDITOR
				Debug.LogWarning("[UIManager] Cannot show Building Selection Panel: not assigned");
#endif
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
#if UNITY_EDITOR
				Debug.Log("[UIManager] Unit Selection Panel shown");
#endif
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
#if UNITY_EDITOR
				Debug.Log($"[UIManager] Settings Panel {(isActive ? "hidden" : "shown")}");
#endif
			}
		}

		/// <summary>
		/// Callback per il bottone "Salva Partita" nell'UI (S3-08).
		/// Chiamato da: UI Button (Settings Panel > Save Button)
		/// </summary>
		public void OnSaveButtonClicked()
		{
			if (_saveManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[UIManager] SaveManager non iniettato - impossibile salvare");
#endif
				return;
			}

			try
			{
				var saveData = _saveManager.Save();
				if (saveData != null)
				{
#if UNITY_EDITOR
					Debug.Log("[UIManager] ✓ Salvataggio manuale completato");
#endif
				}
				else
				{
#if UNITY_EDITOR
					Debug.LogWarning("[UIManager] Salvataggio manuale fallito - dati non validi");
#endif
				}
			}
			catch (System.Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[UIManager] Errore durante salvataggio manuale: {ex.Message}");
#endif
			}
		}

		/// <summary>
		/// Callback per il bottone "Carica Partita" nell'UI (S3-08).
		/// Chiamato da: UI Button (Settings Panel > Load Button)
		/// </summary>
		public void OnLoadButtonClicked()
		{
			if (_saveManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[UIManager] SaveManager non iniettato - impossibile caricare");
#endif
				return;
			}

			try
			{
				var saveData = _saveManager.Load();
				if (saveData != null)
				{
#if UNITY_EDITOR
					Debug.Log("[UIManager] ✓ Caricamento manuale completato");
#endif
				}
				else
				{
#if UNITY_EDITOR
					Debug.LogWarning("[UIManager] Nessun salvataggio trovato o dati non validi");
#endif
				}
			}
			catch (System.Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[UIManager] Errore durante caricamento manuale: {ex.Message}");
#endif
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
#if UNITY_EDITOR
			Debug.Log("[UIManager] DEBUG: All panels shown");
#endif
		}

		[ContextMenu("Debug: Hide All Panels")]
		private void DebugHideAllPanels()
		{
			HideAllOptionalPanels();
#if UNITY_EDITOR
			Debug.Log("[UIManager] DEBUG: All panels hidden");
#endif
		}
#endif

		#endregion
	}
}
