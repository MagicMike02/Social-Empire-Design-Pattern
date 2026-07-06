using Script.Core.SaveSystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Script.UI
{
	/// <summary>
	/// Handles the Settings panel UI, including Save/Load button wiring and panel visibility.
	/// Extracted from UIManager to follow SRP.
	/// </summary>
	public class SettingsPanelController : MonoBehaviour, IPanelToggle
	{
		[SerializeField] private GameObject _settingsPanel; // Assigned via Inspector
		private SaveManager _saveManager;
		private Button _saveButton;
		private Button _loadButton;

		[Inject]
		public void Construct(SaveManager saveManager)
		{
			_saveManager = saveManager;
		}

		private void Awake()
		{
			if (_settingsPanel == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning("[SettingsPanelController] Settings panel GameObject not assigned.");
#endif
				return;
			}

			// Find buttons inside the panel
			var saveBtnTransform = _settingsPanel.transform.Find("SaveButton");
			var loadBtnTransform = _settingsPanel.transform.Find("LoadButton");

			if (saveBtnTransform != null)
			{
				_saveButton = saveBtnTransform.GetComponent<Button>();
				if (_saveButton != null)
					_saveButton.onClick.AddListener(OnSaveButtonClicked);
			}

			if (loadBtnTransform != null)
			{
				_loadButton = loadBtnTransform.GetComponent<Button>();
				if (_loadButton != null)
					_loadButton.onClick.AddListener(OnLoadButtonClicked);
			}
		}

		public void Show()
		{
			if (_settingsPanel != null)
				_settingsPanel.SetActive(true);
		}

		public void Hide()
		{
			if (_settingsPanel != null)
				_settingsPanel.SetActive(false);
		}

		public void Toggle()
		{
			if (_settingsPanel != null)
				_settingsPanel.SetActive(!_settingsPanel.activeSelf);
		}

		// Called by the Save button
		private void OnSaveButtonClicked()
		{
			if (_saveManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[SettingsPanelController] SaveManager not injected – cannot save.");
#endif
				return;
			}

			try
			{
				var data = _saveManager.Save();
#if UNITY_EDITOR
				if (data != null)
					Debug.Log("[SettingsPanelController] ✓ Manual save completed");
				else
					Debug.LogWarning("[SettingsPanelController] Manual save failed – no data returned");
#endif
			}
			catch (System.Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[SettingsPanelController] Error during manual save: {ex.Message}");
#endif
			}
		}

		// Called by the Load button
		private void OnLoadButtonClicked()
		{
			if (_saveManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[SettingsPanelController] SaveManager not injected – cannot load.");
#endif
				return;
			}

			try
			{
				var data = _saveManager.Load();
#if UNITY_EDITOR
				if (data != null)
					Debug.Log("[SettingsPanelController] ✓ Manual load completed");
				else
					Debug.LogWarning("[SettingsPanelController] No save data found or invalid.");
#endif
			}
			catch (System.Exception ex)
			{
#if UNITY_EDITOR
				Debug.LogError($"[SettingsPanelController] Error during manual load: {ex.Message}");
#endif
			}
		}

		private void OnDestroy()
		{
			if (_saveButton != null) _saveButton.onClick.RemoveListener(OnSaveButtonClicked);
			if (_loadButton != null) _loadButton.onClick.RemoveListener(OnLoadButtonClicked);
		}


	}
}
