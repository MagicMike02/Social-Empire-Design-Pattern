using System.Collections.Generic;
using Script.BuildingSystem;
using Script.InputSystem;
using Script.UnitSystem.Config;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Script.UnitSystem
{
    /// <summary>
    /// Traduce input selezione (click e drag box) in operazioni sul UnitSelectionService.
    /// </summary>
    public sealed class UnitSelectionInputHandler : MonoBehaviour
    {
        #region Dependencies

        private InputManager _inputManager;
        private UnitSelectionService _selectionService;
        private BuildingPlacer _buildingPlacer;
        private Camera _camera;

        [Inject]
        public void Construct(
            InputManager inputManager,
            UnitSelectionService selectionService,
            BuildingPlacer buildingPlacer,
            Camera camera)
        {
            _inputManager = inputManager;
            _selectionService = selectionService;
            _buildingPlacer = buildingPlacer;
            _camera = camera;
        }

        #endregion

        #region Inspector

        [Header("Input Actions")]
        [SerializeField] private InputActionReference _pointAction;
        [SerializeField] private InputActionReference _dragSelectAction;
        [SerializeField] private InputActionReference _additiveSelectAction;

        [Header("Selection Box")]
        [SerializeField] private bool _debugDrawSelectionBox;

        [Header("Config")]
        [SerializeField] private UnitSystemConfigSO _config;

        #endregion

        #region State

        private bool _isDragging;
        private Vector2 _dragStartScreen;
        private Vector2 _dragEndScreen;

        private Collider2D[] _buffer;
        private int _overlapBufferSize = 256;
        private float _dragMinSqrDistance = 16f;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            ApplyConfig(_config);

            if (_buffer == null || _buffer.Length != _overlapBufferSize)
            {
                _buffer = new Collider2D[_overlapBufferSize];
            }

            _pointAction?.action?.Enable();
            _dragSelectAction?.action?.Enable();
            _additiveSelectAction?.action?.Enable();

            if (_inputManager != null)
            {
                _inputManager.OnWorldLeftClicked += HandleWorldLeftClick;
            }
        }

        private void OnDisable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnWorldLeftClicked -= HandleWorldLeftClick;
            }

            _pointAction?.action?.Disable();
            _dragSelectAction?.action?.Disable();
            _additiveSelectAction?.action?.Disable();
        }

        private void Update()
        {
            if (_buildingPlacer != null && _buildingPlacer.IsPlacing)
            {
                _isDragging = false;
                return;
            }

            if (_dragSelectAction == null || _dragSelectAction.action == null) return;

            if (_dragSelectAction.action.WasPressedThisFrame())
            {
                _isDragging = true;
                _dragStartScreen = ReadScreenPoint();
                _dragEndScreen = _dragStartScreen;
            }

            if (_isDragging && _dragSelectAction.action.IsPressed())
            {
                _dragEndScreen = ReadScreenPoint();
            }

            if (_isDragging && _dragSelectAction.action.WasReleasedThisFrame())
            {
                _isDragging = false;
                _dragEndScreen = ReadScreenPoint();
                SelectUnitsInDragBox();
            }
        }

        private void OnGUI()
        {
            if (!_debugDrawSelectionBox || !_isDragging) return;

            var rect = GetScreenRect(_dragStartScreen, _dragEndScreen);
            rect.y = Screen.height - rect.y - rect.height;
            GUI.Box(rect, "");
        }

        #endregion

        #region Selection Operations

        private void HandleWorldLeftClick(Vector3 worldPosition, IHoverable hovered)
        {
            if (_buildingPlacer != null && _buildingPlacer.IsPlacing)
            {
                return;
            }

            if (_selectionService == null) return;
            if (_isDragging) return;

            bool additive = IsAdditivePressed();
            var unit = hovered as UnitController;
            _selectionService.SelectSingle(unit, additive);
        }

        private void SelectUnitsInDragBox()
        {
            if (_selectionService == null || _camera == null) return;

            Vector2 min = Vector2.Min(_dragStartScreen, _dragEndScreen);
            Vector2 max = Vector2.Max(_dragStartScreen, _dragEndScreen);

            if ((max - min).sqrMagnitude < _dragMinSqrDistance)
            {
                return;
            }

            Vector3 w0 = _camera.ScreenToWorldPoint(new Vector3(min.x, min.y, 0f));
            Vector3 w1 = _camera.ScreenToWorldPoint(new Vector3(max.x, max.y, 0f));

            var lower = new Vector2(Mathf.Min(w0.x, w1.x), Mathf.Min(w0.y, w1.y));
            var upper = new Vector2(Mathf.Max(w0.x, w1.x), Mathf.Max(w0.y, w1.y));

            int hits = Physics2D.OverlapAreaNonAlloc(lower, upper, _buffer);
            var selectedUnits = new List<UnitController>();

            for (int i = 0; i < hits; i++)
            {
                var col = _buffer[i];
                if (col == null) continue;

                if (col.TryGetComponent<UnitController>(out var unit))
                {
                    selectedUnits.Add(unit);
                }
            }

            bool additive = IsAdditivePressed();
            _selectionService.SelectFromList(selectedUnits, additive);
        }

        private bool IsAdditivePressed()
        {
            return _additiveSelectAction != null && _additiveSelectAction.action != null && _additiveSelectAction.action.IsPressed();
        }

        #endregion

        #region Helpers

        private Vector2 ReadScreenPoint()
        {
            if (_pointAction != null && _pointAction.action != null)
            {
                return _pointAction.action.ReadValue<Vector2>();
            }

            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            return Vector2.zero;
        }

        private static Rect GetScreenRect(Vector2 a, Vector2 b)
        {
            Vector2 topLeft = Vector2.Min(a, b);
            Vector2 bottomRight = Vector2.Max(a, b);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        private void ApplyConfig(UnitSystemConfigSO config)
        {
            if (config == null)
            {
                return;
            }

            _config = config;
            _overlapBufferSize = Mathf.Max(32, config.OverlapBufferSize);
            _dragMinSqrDistance = Mathf.Max(1f, config.DragMinSqrDistance);
        }

        #endregion
    }
}
