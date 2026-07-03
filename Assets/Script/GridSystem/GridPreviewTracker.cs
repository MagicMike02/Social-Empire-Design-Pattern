using System.Collections.Generic;
using UnityEngine;

namespace Script.GridSystem
{
    /// <summary>
    /// Gestisce la preview visiva delle celle senza accoppiarsi a GridManager.
    /// </summary>
    public sealed class GridPreviewTracker
    {
        private readonly TileManager _tileManager;
        private readonly List<Vector2Int> _lastPreviewCells = new();
        private readonly List<Vector2Int> _currentPreviewCells = new();
        private readonly HashSet<Vector2Int> _currentPreviewCellLookup = new();
        private readonly Dictionary<Vector2Int, Tile> _previewTileCache = new();

        public GridPreviewTracker(TileManager tileManager)
        {
            _tileManager = tileManager;
        }

        public void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid)
        {
            ClearPreviousPreview();

            var grid = _tileManager.GetGrid();
            if (grid == null) return;

            _currentPreviewCells.Clear();
            _currentPreviewCellLookup.Clear();

            if (width <= 0 || height <= 0)
            {
                ResetPreviewCaches();
                return;
            }

            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int x = originCell.x + dx;
                    int y = originCell.y + dy;
                    if (x >= 0 && y >= 0 && x < _tileManager.Width && y < _tileManager.Height)
                    {
                        var cell = new Vector2Int(x, y);
                        _currentPreviewCells.Add(cell);
                        _currentPreviewCellLookup.Add(cell);
                    }
                }
            }

            foreach (var cell in _lastPreviewCells)
            {
                if (!_currentPreviewCellLookup.Contains(cell) && _previewTileCache.TryGetValue(cell, out var tile))
                {
                    tile?.ResetTint();
                    _previewTileCache.Remove(cell);
                }
            }

            var color = isValid
                ? new Color(0f, 1f, 0f, 1f)
                : new Color(1f, 0f, 0f, 1f);

            foreach (var cell in _currentPreviewCells)
            {
                if (!_previewTileCache.TryGetValue(cell, out var tile))
                {
                    tile = grid.GetValue(cell.x, cell.y);
                    if (tile != null)
                    {
                        _previewTileCache[cell] = tile;
                    }
                }

                tile?.PreviewTint(color);
            }

            _lastPreviewCells.Clear();
            _lastPreviewCells.AddRange(_currentPreviewCells);
        }

        public void ClearPreview()
        {
            ClearPreviousPreview();
            ResetPreviewCaches();
        }

        private void ClearPreviousPreview()
        {
            var grid = _tileManager.GetGrid();
            if (grid == null) return;

            foreach (var cell in _lastPreviewCells)
            {
                var tile = grid.GetValue(cell.x, cell.y);
                if (tile)
                {
                    tile.ResetTint();
                }
            }

            _lastPreviewCells.Clear();
        }

        private void ResetPreviewCaches()
        {
            _lastPreviewCells.Clear();
            _previewTileCache.Clear();
        }
    }
}
