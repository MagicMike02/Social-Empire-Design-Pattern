using System.Collections.Generic;
using Script.BuildingSystem;
using Script.GridSystem;
using Script.InputSystem;
using Script.UnitSystem.Config;
using Script.UnitSystem.Commands;
using UnityEngine;
using VContainer;

namespace Script.UnitSystem
{
    /// <summary>
    /// Riceve input di comando e costruisce command di movimento per le unita' selezionate.
    /// </summary>
    public sealed class UnitCommandService : MonoBehaviour
    {
        #region Dependencies

        private InputManager _inputManager;
        private UnitSelectionService _selectionService;
        private GridManager _gridManager;
        private BuildingPlacer _buildingPlacer;

        [Header("Config")]
        [SerializeField] private UnitSystemConfigSO _config;
        [SerializeField] private int _maxFormationSearchRadius = 12;

        [Inject]
        public void Construct(
            InputManager inputManager,
            UnitSelectionService selectionService,
            GridManager gridManager,
            BuildingPlacer buildingPlacer)
        {
            _inputManager = inputManager;
            _selectionService = selectionService;
            _gridManager = gridManager;
            _buildingPlacer = buildingPlacer;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            ApplyConfig(_config);

            if (_inputManager != null)
            {
                _inputManager.OnWorldRightClicked += HandleWorldRightClick;
            }
        }

        private void OnDisable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnWorldRightClicked -= HandleWorldRightClick;
            }
        }

        #endregion

        #region Command Dispatch

        private void HandleWorldRightClick(Vector3 worldPosition, IHoverable hovered)
        {
            if (_buildingPlacer != null && _buildingPlacer.IsPlacing)
            {
                return;
            }

            if (_selectionService == null || _gridManager == null)
            {
                return;
            }

            var selected = _selectionService.GetSelectedUnits();
            if (selected == null || selected.Count == 0)
            {
                return;
            }

            if (!_gridManager.TryWorldToCell(worldPosition, out var target3))
            {
                return;
            }

            var target = new Vector2Int(target3.x, target3.y);
            if (!_gridManager.IsCellFree(target))
            {
                return;
            }

            var units = new List<UnitController>(selected.Count);
            units.AddRange(selected);

            List<Vector2Int> destinations = BuildDestinationSlots(units.Count, target);
            if (destinations.Count == 0)
            {
                return;
            }

            if (destinations.Count < units.Count)
            {
                units.RemoveRange(destinations.Count, units.Count - destinations.Count);
            }

            IUnitCommand command = new UnitMoveCommand(units, destinations);
            command.Execute();
        }

        private List<Vector2Int> BuildDestinationSlots(int count, Vector2Int center)
        {
            var result = new List<Vector2Int>(count);
            if (count <= 0) return result;

            if (_gridManager.IsCellFree(center))
            {
                result.Add(center);
            }

            int radius = 1;
            while (result.Count < count && radius < _maxFormationSearchRadius)
            {
                for (int x = -radius; x <= radius && result.Count < count; x++)
                {
                    TryAddSlot(center + new Vector2Int(x, radius), result);
                    if (result.Count >= count) break;
                    TryAddSlot(center + new Vector2Int(x, -radius), result);
                }

                for (int y = -radius + 1; y <= radius - 1 && result.Count < count; y++)
                {
                    TryAddSlot(center + new Vector2Int(radius, y), result);
                    if (result.Count >= count) break;
                    TryAddSlot(center + new Vector2Int(-radius, y), result);
                }

                radius++;
            }

            return result;
        }

        private void TryAddSlot(Vector2Int cell, List<Vector2Int> slots)
        {
            if (_gridManager == null) return;
            if (cell.x < 0 || cell.y < 0 || cell.x >= _gridManager.Width || cell.y >= _gridManager.Height) return;
            if (!_gridManager.IsCellFree(cell)) return;
            if (slots.Contains(cell)) return;

            slots.Add(cell);
        }

        private void ApplyConfig(UnitSystemConfigSO config)
        {
            if (config == null)
            {
                return;
            }

            _config = config;
            _maxFormationSearchRadius = Mathf.Max(2, config.MaxFormationSearchRadius);
        }

        #endregion
    }
}
