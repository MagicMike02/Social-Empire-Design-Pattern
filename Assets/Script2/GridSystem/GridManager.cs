﻿using Script2.Economy;
using Script2.ResourceSystem;
using UnityEngine;
using Script2.BuildingSystem;
using System.Collections.Generic;

namespace Script2.GridSystem
{
    /// <summary>
    /// Gestisce la griglia di gioco, integrando TileManager e ZoneManager.
    /// Implementa IGridService per fornire operazioni sulla griglia al BuildingSystem.
    /// </summary>
    public class GridManager : MonoBehaviour, IGridService
    {
        public static GridManager Instance { get; private set; }

        [SerializeField] private TileManager _tileManager;
        [SerializeField] private ZoneManager _zoneManager;
        [SerializeField] private ResourceSpawner _resourceSpawner;

        private readonly List<Vector2Int> _lastPreviewCells = new();
        private readonly HashSet<Vector2Int> _occupiedCells = new();

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Debug.LogWarning("[GridManager] Istanza duplicata rilevata e distrutta.");
                Destroy(gameObject);
                return;
            }

            ValidateDependencies();
            InitializeGrid();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void ValidateDependencies()
        {
            if (_tileManager == null)
            {
                Debug.LogError("[GridManager] TileManager non assegnato nell'Inspector! Assegna il riferimento per evitare errori di runtime.");
            }

            if (_zoneManager == null)
            {
                Debug.LogError("[GridManager] ZoneManager non assegnato nell'Inspector! Assegna il riferimento per evitare errori di runtime.");
            }
        }

        private void InitializeGrid()
        {
            if (_tileManager == null) return;
            
            // Inizializza la griglia tramite TileManager
            _tileManager.CreateGrid();
            var grid = _tileManager.GetGrid();
            
            if (_zoneManager != null)
            {
                _zoneManager.Initialize(grid);
                _zoneManager.CreateZones(_tileManager.Width, _tileManager.Height);
            }
        }

        // IGridService implementation
        public bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell)
        {
            var grid = _tileManager.GetGrid();
            grid.GetWorldToIsoPosition(worldPos, out int x, out int y);
            cell = new Vector3Int(x, y, 0);
            // Limita ai bounds
            bool inside = x >= 0 && y >= 0 && x < _tileManager.Width && y < _tileManager.Height;
            return inside;
        }

        public Vector3 CellToWorld(Vector3Int cell)
        {
            var grid = _tileManager.GetGrid();
            return grid.GetIsoToWorldPosition(cell.x, cell.y);
        }

        public bool AreCellsFree(Vector3Int originCell, int width, int height)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int x = originCell.x + dx;
                    int y = originCell.y + dy;
                    if (x < 0 || y < 0 || x >= _tileManager.Width || y >= _tileManager.Height) return false;

                    var p = new Vector2Int(x, y);
                    if (_occupiedCells.Contains(p)) return false;

                    var tile = _tileManager.GetGrid().GetValue(x, y);
                    if (tile == null || tile.State != TileState.Unlocked) return false;
                }
            }
            return true;
        }

        public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var p = new Vector2Int(originCell.x + dx, originCell.y + dy);
                    _occupiedCells.Add(p);
                }
            }
        }

        public void FreeCells(Vector3Int originCell, int width, int height)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var p = new Vector2Int(originCell.x + dx, originCell.y + dy);
                    _occupiedCells.Remove(p);
                }
            }
        }

        public void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid)
        {
            // reset celle precedenti
            if (_lastPreviewCells.Count > 0)
            {
                foreach (var p in _lastPreviewCells)
                {
                    var tilePrev = _tileManager.GetGrid().GetValue(p.x, p.y);
                    if (tilePrev != null) tilePrev.ResetTint();
                }
                _lastPreviewCells.Clear();
            }

            // richiesta di clear-only
            if (width <= 0 || height <= 0) return;

            var color = isValid ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int x = originCell.x + dx;
                    int y = originCell.y + dy;
                    if (x < 0 || y < 0 || x >= _tileManager.Width || y >= _tileManager.Height) continue;
                    var tile = _tileManager.GetGrid().GetValue(x, y);
                    if (tile != null)
                    {
                        tile.PreviewTint(color);
                        _lastPreviewCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
    }
}