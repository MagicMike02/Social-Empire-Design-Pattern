using Script.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Script.BuildingSystem
{
	/// <summary>
	/// Controller per la preview visiva del piazzamento edifici (grid + prefab).
	/// Estratto da <see cref="BuildingPlacer"/> per rispettare il Single Responsibility Principle.
	/// Gestisce: mouse tracking, conversione screen→world→cell, aggiornamento preview.
	/// </summary>
	public sealed class BuildingPreviewController
	{
		#region Dependencies

		private readonly Camera _camera;
		private readonly GenericPreviewSystem _previewSystem;
		private readonly BuildingManager _manager;
		private readonly BuildingValidationService _validationService;
		private readonly BuildingPlacementStateTracker _state;
		private readonly InputActionReference _pointAction;

		#endregion

		#region Properties

		/// <summary>
		/// Indica se il controller ha una configurazione selezionata attiva.
		/// </summary>
		public bool HasSelectedConfig => _state.SelectedConfig != null;

		#endregion

		#region Initialization

		public BuildingPreviewController(
			Camera camera,
			GenericPreviewSystem previewSystem,
			BuildingManager manager,
			BuildingValidationService validationService,
			BuildingPlacementStateTracker state,
			InputActionReference pointAction)
		{
			_camera = camera;
			_previewSystem = previewSystem;
			_manager = manager;
			_validationService = validationService;
			_state = state;
			_pointAction = pointAction;
		}

		#endregion

		#region Public API

		/// <summary>
		/// Pulisce le preview visive sulla vecchia griglia e prefab.
		/// Chiamato da BuildingPlacer.SetSelectedConfig prima di assegnare la nuova config.
		/// </summary>
		public void CleanupBeforeNewSelection()
		{
			CleanupGridPreview();

			if (_previewSystem != null)
			{
				_previewSystem.HidePreview();
			}
		}

		/// <summary>
		/// Pulisce completamente la preview (grid + prefab + state).
		/// Chiamato da IdleState.OnEnter.
		/// </summary>
		public void ClearPreview()
		{
			if (_previewSystem != null)
			{
				_previewSystem.HidePreview();
			}

			CleanupGridPreview();

			_state.Clear();
		}

		/// <summary>
		/// Aggiorna la posizione della preview seguendo il mouse.
		/// Chiamato da PreviewingState.OnUpdate.
		/// </summary>
		public void UpdatePlacementPreview()
		{
			var selectedConfig = _state.SelectedConfig;
			if (_camera == null || _manager?.Grid == null || selectedConfig == null || _previewSystem == null)
			{
				return;
			}

			// STEP 1: Conversione mouse → world
			var mousePos = ReadMousePosition();
			var worldPos = _camera.ScreenToWorldPoint(mousePos);
			worldPos.z = 1f;

			// STEP 2: Converti a cella della griglia
			if (!_manager.Grid.TryWorldToCell(worldPos, out var cell))
			{
				return;
			}

			// STEP 3: Se cella non è cambiata, non aggiornare (ottimizzazione)
			if (cell == _state.LastPreviewCell)
			{
				return;
			}

			_state.SetCurrentCell(cell);

			// Calcola posizione world snappata per il building finale
			var snapPos = _manager.Grid.CellToWorld(cell);

			// STEP 5: Validazione (il building può essere piazzato qui?)
			bool isValid = _validationService.CanPlaceBuilding(selectedConfig, cell);

			// STEP 6: Aggiorna il preview visivo del building con posizione snappata
			bool updated = _previewSystem.UpdatePreviewIfCellChanged(cell, snapPos, isValid);

			// Se prima volta o cella cambiata, mostra preview
			if (_state.LastPreviewCell.x == -1000 || updated)
			{
				_previewSystem.ShowPreview(selectedConfig.Prefab, snapPos, isValid);
			}

			// Aggiorna preview griglia tile
			if (updated || _state.LastValidState != isValid)
			{
				_manager.Grid.SetCellsPreview(cell, selectedConfig.Width, selectedConfig.Height, isValid);
				_state.MarkPreviewState(cell, isValid);
			}
		}

		/// <summary>
		/// Pulisce solo la preview della griglia (tile highlight).
		/// </summary>
		public void CleanupGridPreview()
		{
			if (_manager?.Grid != null && _state.SelectedConfig != null)
			{
				_manager.Grid.SetCellsPreview(_state.CurrentCell, 0, 0, false);
			}
		}

		#endregion

		#region Private Methods

		private Vector3 ReadMousePosition()
		{
			if (_pointAction != null && _pointAction.action != null)
			{
				Vector2 screenPos = _pointAction.action.ReadValue<Vector2>();
				return new Vector3(screenPos.x, screenPos.y, 0f);
			}

			if (Mouse.current != null)
			{
				Vector2 screenPos = Mouse.current.position.ReadValue();
				return new Vector3(screenPos.x, screenPos.y, 0f);
			}

			return Vector3.zero;
		}

		#endregion
	}
}
