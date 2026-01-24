﻿using UnityEngine;
using VContainer;
using Script2.BuildingSystem;
using Script2.Core;

namespace Script2.InputSystem
{
    // Centralized input handler: single non-alloc raycast per frame
    // Priority: Unit > Building > Resource > ZoneSign > Tile
    // GC-friendly: reuse arrays, avoid Linq/allocations in Update
    public sealed class InputManager : MonoBehaviour
    {
        [Inject] private Camera _camera;
        [Inject] private IGridService _grid;

        [SerializeField] private bool _debugMode = false;

        // Non-alloc hit buffers (tunable size)
        private const int MaxHits = 8;
        private readonly RaycastHit2D[] _hitsBuffer = new RaycastHit2D[MaxHits];

        private IHoverable _lastHovered;
        private Vector3 _lastMousePos;

        private void Update()
        {
            var mousePos = Input.mousePosition;
            
            // Micro-optimization: skip if mouse hasn't moved
            if (mousePos == _lastMousePos) return;
            _lastMousePos = mousePos;

            // Screen → Ray
            var ray = _camera.ScreenPointToRay(mousePos);

            // Non-alloc raycast (ALL layers), then apply priority filter manually
            int hitCount = Physics2D.RaycastNonAlloc(ray.origin, ray.direction, _hitsBuffer, Mathf.Infinity);

            if (_debugMode && hitCount > 0)
            {
                Debug.Log($"[InputManager] Raycast hits: {hitCount}");
                for (int i = 0; i < hitCount; i++)
                {
                    Debug.Log($"  [{i}] {_hitsBuffer[i].collider.gameObject.name} (layer {_hitsBuffer[i].collider.gameObject.layer})");
                }
            }

            IHoverable target = null;
            Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);

            // Priority search: Unit > Building > Resource > ZoneSign > Tile
            target = FindFirstHoverable(_hitsBuffer, hitCount, LayerRegistry.Unit)
                  ?? FindFirstHoverable(_hitsBuffer, hitCount, LayerRegistry.Building)
                  ?? FindFirstHoverable(_hitsBuffer, hitCount, LayerRegistry.Resource)
                  ?? FindFirstHoverable(_hitsBuffer, hitCount, LayerRegistry.ZoneSign)
                  ?? FindFirstHoverable(_hitsBuffer, hitCount, LayerRegistry.Tile);

            // Handle hover transitions
            if (target != _lastHovered)
            {
                _lastHovered?.OnHoverExit();
                target?.OnHoverEnter();
                _lastHovered = target;
                
                if (target != null)
                    Debug.Log($"[InputManager] Hovering: {target.GetType().Name}");
            }

            // Clicks
            if (Input.GetMouseButtonDown(0))
            {
                target?.OnClick();
                if (target != null)
                    Debug.Log($"[InputManager] Click on {target.GetType().Name}");
            }
            if (Input.GetMouseButtonDown(1))
            {
                // Right-click passes precise world position (used for commands)
                target?.OnRightClick(worldPos);
                if (target != null)
                    Debug.Log($"[InputManager] RightClick on {target.GetType().Name} at {worldPos}");
            }
        }

        private IHoverable FindFirstHoverable(RaycastHit2D[] hits, int hitCount, int layerId)
        {
            if (layerId == -1) return null; // Layer non esiste
            
            for (int i = 0; i < hitCount; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                
                // Semplice confronto diretto: il layer del gameobject deve corrispondere
                if (col.gameObject.layer != layerId) continue;
                
                var hover = col.GetComponent<IHoverable>();
                if (_debugMode)
                {
                    Debug.Log($"[InputManager] Checking {col.gameObject.name}: IHoverable = {(hover != null ? "YES" : "NO")}");
                }
                
                if (hover != null) return hover;
            }
            return null;
        }
    }
}
