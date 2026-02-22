using UnityEngine;
using VContainer;
using Script.BuildingSystem;
using System;
using Script.Core;
using Script.GridSystem;
using UnityEngine.EventSystems;

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

        private IHoverable _lastHovered;
        private Vector3 _lastMousePos;
        
        #endregion

        #region Unity Lifecycle

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

            var mousePos = Input.mousePosition;
            bool mouseMoved = mousePos != _lastMousePos;

            if (mouseMoved)
            {
                _lastMousePos = mousePos;
                Vector3 wp = _camera.ScreenToWorldPoint(mousePos);
                Vector2 point = new Vector2(wp.x, wp.y);

                int hitCount = Physics2D.OverlapPointNonAlloc(point, _overlapBuffer);
                if (_debugMode && hitCount > 0)
                {
                    Debug.Log($"[InputManager] OverlapPoint hits: {hitCount}");
                    for (int i = 0; i < hitCount; i++)
                    {
                        if (_overlapBuffer[i])
                            Debug.Log($"  [{i}] {_overlapBuffer[i].gameObject.name} (layer {_overlapBuffer[i].gameObject.layer})");
                    }
                }

                IHoverable target = FindFirstHoverable(_overlapBuffer, hitCount, LayerRegistry.Unit)
                                 ?? FindFirstHoverable(_overlapBuffer, hitCount, LayerRegistry.Building)
                                 ?? FindFirstHoverable(_overlapBuffer, hitCount, LayerRegistry.Resource)
                                 ?? FindFirstHoverable(_overlapBuffer, hitCount, LayerRegistry.ZoneSign)
                                 ?? FindFirstHoverable(_overlapBuffer, hitCount, LayerRegistry.Tile);

                if (target != _lastHovered)
                {
                    _lastHovered?.OnHoverExit();
                    target?.OnHoverEnter();
                    _lastHovered = target;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                _lastHovered?.OnClick();
                if (_debugMode && _lastHovered != null) 
                    Debug.Log($"[InputManager] Click on {_lastHovered.GetType().Name}");

                // Collider-First: pass the Tile itself, not a calculated cell
                // Subscriber reads tile.GridPosition (cached at Initialize, authoritative Source of Truth)
                if (_lastHovered is Tile tile)
                {
                    OnTileClicked?.Invoke(tile);
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                Vector3 wp = _camera.ScreenToWorldPoint(_lastMousePos);
                _lastHovered?.OnRightClick(wp);
                OnMapRightClicked?.Invoke();
                if (_debugMode && _lastHovered != null) 
                    Debug.Log($"[InputManager] RightClick on {_lastHovered.GetType().Name} at {wp}");
            }
        }
        
        #endregion

        #region Private Helpers

        /// <summary>
        /// Trova e restituisce il primo component IHoverable in un array di collider filtrati per layermask.
        /// </summary>
        private IHoverable FindFirstHoverable(Collider2D[] colliders, int count, int layerId)
        {
            if (layerId == -1) return null;
            for (int i = 0; i < count; i++)
            {
                var col = colliders[i];
                if (col == null) continue;
                if (col.gameObject.layer != layerId) continue;

                var hover = col.GetComponent<IHoverable>();
                if (_debugMode && hover != null)
                    Debug.Log($"[InputManager] {col.gameObject.name} is IHoverable");
                
                if (hover != null) return hover;
            }
            return null;
        }
        
        #endregion
    }
}
