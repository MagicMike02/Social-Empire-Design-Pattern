using UnityEngine;
using VContainer;
using Script2.InputSystem;
using Script2.BuildingSystem;

namespace Script2.InputSystem
{
    // Centralized input handler: single non-alloc raycast per frame
    // Priority: Unit > Building > Tile
    // GC-friendly: reuse arrays, avoid Linq/allocations in Update
    public sealed class InputManager : MonoBehaviour
    {
        [Inject] private Camera _camera;
        [Inject] private IGridService _grid;

        [SerializeField] private LayerMask _unitMask;
        [SerializeField] private LayerMask _buildingMask;
        [SerializeField] private LayerMask _tileMask;

        // Non-alloc hit buffers (tunable size)
        private const int MaxHits = 8;
        private readonly RaycastHit2D[] _hitsBuffer = new RaycastHit2D[MaxHits];

        private IHoverable _lastHovered;
        private Vector3 _lastMousePos;

        private void Update()
        {
            var mousePos = Input.mousePosition;
            // Optional micro-optimization: skip if mouse hasn't moved
            // if (mousePos == _lastMousePos) return;
            _lastMousePos = mousePos;

            // Screen → Ray
            var ray = _camera.ScreenPointToRay(mousePos);

            // Non-alloc raycast (ALL layers), then apply priority filter manually
            int hitCount = Physics2D.RaycastNonAlloc(ray.origin, ray.direction, _hitsBuffer, Mathf.Infinity);

            IHoverable target = null;
            Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);

            // Priority search: Unit > Building > Tile
            // First pass: Units
            target = FindFirstHoverable(_hitsBuffer, hitCount, _unitMask);
            if (target == null)
            {
                // Second pass: Buildings
                target = FindFirstHoverable(_hitsBuffer, hitCount, _buildingMask);
                if (target == null)
                {
                    // Third pass: Tiles
                    target = FindFirstHoverable(_hitsBuffer, hitCount, _tileMask);
                }
            }

            // Handle hover transitions
            if (target != _lastHovered)
            {
                _lastHovered?.OnHoverExit();
                target?.OnHoverEnter();
                _lastHovered = target;
            }

            // Clicks
            if (Input.GetMouseButtonDown(0))
            {
                target?.OnClick();
            }
            if (Input.GetMouseButtonDown(1))
            {
                // Right-click passes precise world position (used for commands)
                target?.OnRightClick(worldPos);
            }
        }

        private IHoverable FindFirstHoverable(RaycastHit2D[] hits, int hitCount, LayerMask mask)
        {
            for (int i = 0; i < hitCount; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                if (((1 << col.gameObject.layer) & mask.value) == 0) continue; // layer not in mask
                var hover = col.GetComponent<IHoverable>();
                if (hover != null) return hover;
            }
            return null;
        }
    }
}
