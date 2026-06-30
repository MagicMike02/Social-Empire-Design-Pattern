using Script.GridSystem;
using Script.UnitSystem.Config;
using UnityEngine;
using VContainer;
using System.Collections.Generic;

namespace Script.UnitSystem.Production
{
    /// <summary>
    /// Factory runtime unificata per spawn unita' da qualsiasi sorgente.
    /// </summary>
    public sealed class UnitSpawnService : MonoBehaviour
    {
        private GridManager _gridManager;
        private UnitUnlockService _unlockService;

        [SerializeField] private UnitSystemConfigSO _config;
        [SerializeField] private Transform _unitRoot;

        [Inject]
        public void Construct(GridManager gridManager, UnitUnlockService unlockService)
        {
            _gridManager = gridManager;
            _unlockService = unlockService;
        }

        public bool TrySpawnUnits(UnitSpawnRequest request, out int spawnedCount)
        {
            spawnedCount = 0;

            if (_config == null || _config.UnitCatalog == null)
            {
                return false;
            }

            if (!_config.UnitCatalog.TryGetById(request.UnitId, out var unitConfig) || unitConfig == null)
            {
                return false;
            }

            if (!request.IgnoreUnlock && _unlockService != null && !_unlockService.IsUnlocked(request.UnitId))
            {
                return false;
            }

            int count = Mathf.Max(1, request.Count);
            int level = Mathf.Max(1, request.Level);
            int radius = request.SearchRadius > 0 ? request.SearchRadius : 8;
            var reservedCells = new HashSet<Vector2Int>();

            Vector3 baseWorld = request.UseWorldPosition ? request.WorldPosition : Vector3.zero;
            if (!request.UseWorldPosition)
            {
                if (_gridManager != null)
                {
                    baseWorld = _gridManager.CellToWorld(Vector3Int.zero);
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (!TryFindSpawnCell(baseWorld, radius, reservedCells, out var spawnCell, out var worldPos))
                {
                    continue;
                }

                var parent = request.ParentOverride != null ? request.ParentOverride : _unitRoot;
                var prefab = unitConfig.Prefab;
                if (prefab == null)
                {
                    continue;
                }

                var go = Instantiate(prefab, worldPos, Quaternion.identity, parent);
                if (_gridManager != null)
                {
                    _gridManager.OccupyCell(spawnCell, go);
                    reservedCells.Add(spawnCell);
                }

                var unit = go.GetComponent<UnitController>();
                if (unit == null)
                {
                    unit = go.AddComponent<UnitController>();
                }

                unit.ApplyConfig(unitConfig, level);
                spawnedCount++;
            }

            return spawnedCount > 0;
        }

        private bool TryFindSpawnCell(Vector3 baseWorld, int maxRadius, HashSet<Vector2Int> reservedCells, out Vector2Int spawnCell, out Vector3 spawnWorld)
        {
            spawnCell = default;
            spawnWorld = baseWorld;

            if (_gridManager == null)
            {
                return true;
            }

            if (!_gridManager.TryWorldToCell(baseWorld, out var baseCell3))
            {
                return false;
            }

            var center = new Vector2Int(baseCell3.x, baseCell3.y);
            if (_gridManager.IsCellFree(center) && (reservedCells == null || !reservedCells.Contains(center)))
            {
                spawnCell = center;
                spawnWorld = _gridManager.CellToWorld(new Vector3Int(center.x, center.y, 0));
                return true;
            }

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (TryCell(center + new Vector2Int(x, radius), reservedCells, out spawnCell, out spawnWorld)) return true;
                    if (TryCell(center + new Vector2Int(x, -radius), reservedCells, out spawnCell, out spawnWorld)) return true;
                }

                for (int y = -radius + 1; y <= radius - 1; y++)
                {
                    if (TryCell(center + new Vector2Int(radius, y), reservedCells, out spawnCell, out spawnWorld)) return true;
                    if (TryCell(center + new Vector2Int(-radius, y), reservedCells, out spawnCell, out spawnWorld)) return true;
                }
            }

            return false;
        }

        private bool TryCell(Vector2Int cell, HashSet<Vector2Int> reservedCells, out Vector2Int spawnCell, out Vector3 world)
        {
            spawnCell = default;
            world = default;

            if (_gridManager == null) return false;
            if (cell.x < 0 || cell.y < 0 || cell.x >= _gridManager.Width || cell.y >= _gridManager.Height) return false;
            if (!_gridManager.IsCellFree(cell)) return false;
            if (reservedCells != null && reservedCells.Contains(cell)) return false;

            spawnCell = cell;
            world = _gridManager.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
            return true;
        }
    }
}
