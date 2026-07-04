using UnityEngine;
using VContainer;
using Script.BuildingSystem;
using System;
using Script.Core;
using Script.GridSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Script.InputSystem
{
    /// <summary>
    /// Centralized input handler for grid-based interactions.
    /// Uses Collider-First approach: raycast hits determine object identity and grid position (Source of Truth).
    /// No mathematical projections for cell detection - relys on IGridEntity.GridPosition from collider hit.
    /// Priority: Unit > Building > Resource > ZoneSign > Tile.
    /// GC-friendly: reuse arrays, avoid allocations in Update.
    /// </summary>
    public sealed class InputManager : MonoBehaviour
    {
        #region External Dependencies
        
        [Inject] private Camera _camera;
        
        #endregion

        #region Inspector Fields
        
        [SerializeField] private bool _debugMode;
        [Header("Input Actions")]
        [SerializeField] private InputActionReference _pointAction;
        [SerializeField] private InputActionReference _leftClickAction;
        [SerializeField] private InputActionReference _rightClickAction;
        
        #endregion

        #region Events

        /// <summary>
        /// Evento notificato al click su un tile valido.
        /// Trasporta il Tile (Source of Truth) anziche' le coordinate logiche.
        /// </summary>
        public event Action<Tile> OnTileClicked;

        /// <summary>
        /// Evento globale notificato al click destro sulla griglia.
        /// </summary>
        public event Action OnMapRightClicked;
        
        #endregion
        
        #region Constants & Private Fields

        // Non-alloc overlap buffers (tunable size)
        private const int MaxOverlaps = 16;
        private readonly Collider2D[] _overlapBuffer = new Collider2D[MaxOverlaps];
        private static readonly ContactFilter2D OverlapFilter = ContactFilter2D.noFilter;

        private IHoverable _lastHovered;
        private Vector3 _lastMousePos;
        
        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EnableAction(_pointAction);
            EnableAction(_leftClickAction);
            EnableAction(_rightClickAction);
        }

        private void OnDisable()
        {
            DisableAction(_pointAction);
            DisableAction(_leftClickAction);
            DisableAction(_rightClickAction);
        }

        private void Update()
        {
            // Evita interazioni sulla World Grid se il cursore si trova sopra UI Elements 
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (_lastHovered != null)
                {
                    _lastHovered.OnHoverExit();
                    _lastHovered = null;
                }
                return;
            }

            var mousePos = ReadMousePosition();
            bool mouseMoved = mousePos != _lastMousePos;

            if (mouseMoved)
            {
                _lastMousePos = mousePos;
                Vector3 wp = _camera.ScreenToWorldPoint(mousePos);
                Vector2 point = new Vector2(wp.x, wp.y);

                int hitCount = Physics2D.OverlapPoint(point, OverlapFilter, _overlapBuffer);
                if (_debugMode && hitCount > 0)
                {
#if UNITY_EDITOR
                    Debug.Log($"[InputManager] OverlapPoint hits: {hitCount}");
                    for (int i = 0; i < hitCount; i++)
                    {
                        if (_overlapBuffer[i])
                            Debug.Log($"  [{i}] {_overlapBuffer[i].gameObject.name} (layer {_overlapBuffer[i].gameObject.layer})");
                    }
#endif
                }

                // Unica chiamata ottimizzata che filtra per priorità decrescente con TryGetComponent O(1) in allocazioni
                IHoverable target = FindFirstHoverableWithPriority(hitCount);

                if (target != _lastHovered)
                {
                    _lastHovered?.OnHoverExit();
                    target?.OnHoverEnter();
                    _lastHovered = target;
                }
            }

            if (_leftClickAction != null && _leftClickAction.action != null && _leftClickAction.action.WasPressedThisFrame())
            {
                _lastHovered?.OnClick();
                if (_debugMode && _lastHovered != null) 
#if UNITY_EDITOR
                    Debug.Log($"[InputManager] Click on {_lastHovered.GetType().Name}");
#endif

                // Collider-First: pass the Tile itself, not a calculated cell
                // Subscriber reads tile.GridPosition (cached at Initialize, authoritative Source of Truth)
                if (_lastHovered is Tile tile)
                {
                    OnTileClicked?.Invoke(tile);
                }
            }
            if (_rightClickAction != null && _rightClickAction.action != null && _rightClickAction.action.WasPressedThisFrame())
            {
                Vector3 wp = _camera.ScreenToWorldPoint(_lastMousePos);
                _lastHovered?.OnRightClick(wp);
                OnMapRightClicked?.Invoke();
                if (_debugMode && _lastHovered != null) 
#if UNITY_EDITOR
                    Debug.Log($"[InputManager] RightClick on {_lastHovered.GetType().Name} at {wp}");
#endif
            }
        }
        
        #endregion

        #region Private Helpers

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

            return _lastMousePos;
        }

        private static void EnableAction(InputActionReference actionReference)
        {
            actionReference?.action?.Enable();
        }

        private static void DisableAction(InputActionReference actionReference)
        {
            actionReference?.action?.Disable();
        }

        /// <summary>
        /// Trova e restituisce il primo component IHoverable in base all'ordine di priorità stabilito.
        /// Ottimizzato con TryGetComponent per abbattere le Micro-Allocazioni in Update.
        /// </summary>
        private IHoverable FindFirstHoverableWithPriority(int hitCount)
        {
            // Priority: Unit > Building > Resource > ZoneSign > Tile.
            int[] priorityLayers = { LayerRegistry.Unit, LayerRegistry.Building, LayerRegistry.Resource, LayerRegistry.ZoneSign, LayerRegistry.Tile };

            foreach (var layer in priorityLayers)
            {
                if (layer == -1) continue;

                for (int i = 0; i < hitCount; i++)
                {
                    var col = _overlapBuffer[i];
                    if (col != null && col.gameObject.layer == layer)
                    {
                        // TryGetComponent non alloca costrutti null se il Component non esiste
                        if (col.TryGetComponent<IHoverable>(out var hoverable))
                        {
                            if (_debugMode)
#if UNITY_EDITOR
                                Debug.Log($"[InputManager] {col.gameObject.name} is IHoverable");
#endif
                            return hoverable;
                        }
                    }
                }
            }
            return null;
        }
        
        #endregion
    }
}
