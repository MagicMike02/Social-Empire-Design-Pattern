using Script2.ResourceSystem;
using UnityEngine;
using Script2.BuildingSystem;
using System.Collections.Generic;

namespace Script2.GridSystem
{
    /// <summary>
    /// Gestisce la griglia di gioco, integrando TileManager e ZoneManager.
    /// REFACTORED: Usa Dependency Injection invece di Singleton pattern.
    /// Implementa IGridService per fornire operazioni sulla griglia al BuildingSystem.
    /// </summary>
    public class GridManager : MonoBehaviour, IGridService
    {
        [SerializeField] private TileManager _tileManager;
        [SerializeField] private ZoneManager _zoneManager;
        [SerializeField] private ResourceSpawner _resourceSpawner;

        private readonly List<Vector2Int> _lastPreviewCells = new();
        private readonly HashSet<Vector2Int> _occupiedCells = new();
        private readonly Dictionary<Vector2Int, Tile> _previewTileCache = new(); 

        private void Awake()
        {
            ValidateDependencies();
            InitializeGrid();
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

            if (_zoneManager == null) return;
            
            _zoneManager.Initialize(grid);
            _zoneManager.CreateZones(_tileManager.Width, _tileManager.Height);
        }

        // IGridService implementation
        public bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell)
        {
            var grid = _tileManager.GetGrid();
            grid.GetWorldToIsoPosition(worldPos, out int x, out int y);
            cell = new Vector3Int(x, y, 0);

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
            ClearPreviousPreview();

            // Cache riferimento griglia per performance
            var grid = _tileManager.GetGrid();
            if (grid == null) return;

            var newPreviewCells = new List<Vector2Int>();
            
            // richiesta di clear-only
            if (width <= 0 || height <= 0)
            {
                // Reset solo celle precedenti usando cache
                foreach (var p in _lastPreviewCells)
                {
                    if (!_previewTileCache.TryGetValue(p, out var tile)) continue;

                    if (tile) tile.ResetTint();
                }
                _lastPreviewCells.Clear();
                _previewTileCache.Clear();
                return;
            }

            // Calcola nuove celle da evidenziare
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int x = originCell.x + dx;
                    int y = originCell.y + dy;
                    if (x >= 0 && y >= 0 && x < _tileManager.Width && y < _tileManager.Height)
                    {
                        newPreviewCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            // Reset SOLO celle che non sono più nella preview
            foreach (var p in _lastPreviewCells)
            {
                if (!newPreviewCells.Contains(p))
                {
                    if (_previewTileCache.TryGetValue(p, out var tile))
                    {
                        tile?.ResetTint();
                        _previewTileCache.Remove(p);
                    }
                }
            }

            // Colori PURI che sostituiscono completamente il tile (no alpha, no moltiplicazione)
            var greenPreview = new Color(0f, 1f, 0f, 1f); // Verde puro opaco
            var redPreview = new Color(1f, 0f, 0f, 1f);   // Rosso puro opaco
            
            var color = isValid ? greenPreview : redPreview;
            
            // Applica colore SOLO alle nuove celle usando cache
            foreach (var p in newPreviewCells)
            {
                // Usa cache o recupera e cachea
                if (!_previewTileCache.TryGetValue(p, out var tile))
                {
                    tile = grid.GetValue(p.x, p.y);
                    if (tile != null)
                    {
                        _previewTileCache[p] = tile;
                    }
                }
                
                tile?.PreviewTint(color);
            }

            // Aggiorna cache celle
            _lastPreviewCells.Clear();
            _lastPreviewCells.AddRange(newPreviewCells);
        }
        
        private void ClearPreviousPreview()
        {
            foreach (var cell in _lastPreviewCells)
            {
                var tile = _tileManager.GetGrid().GetValue(cell.x, cell.y);
                if (tile)
                {
                    tile.ResetTint();
                }
            }
            _lastPreviewCells.Clear();
        }

        // Helper: trova la cella il cui centro è più vicino a worldPos (considera 9 candidate attorno a quella grezza)
        private bool TryWorldToNearestCell(Vector3 worldPos, out Vector3Int bestCell)
        {
            bestCell = default;
            // Conversione grezza
            if (!TryWorldToCell(worldPos, out var approx)) return false;

            float bestDist = float.MaxValue;
            var grid = _tileManager.GetGrid();

            // Esamina le 9 celle attorno a approx (inclusa)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int cx = approx.x + dx;
                    int cy = approx.y + dy;
                    if (cx < 0 || cy < 0 || cx >= _tileManager.Width || cy >= _tileManager.Height) continue;

                    var candidate = new Vector3Int(cx, cy, 0);

                    // Centro VISIVO del tile (non il pivot): usa SpriteRenderer.bounds.center se disponibile
                    Vector3 center;
                    var tile = grid.GetValue(cx, cy);
                    if (tile != null)
                    {
                        var sr = tile.GetComponent<SpriteRenderer>();
                        center = sr != null ? sr.bounds.center : tile.transform.position;
                    }
                    else
                    {
                        // Fallback: centro matematico della cella
                        center = CellToWorld(candidate);
                    }

                    // Distanza 2D su piano di gioco (x,y)
                    float d2 = (new Vector2(center.x, center.y) - new Vector2(worldPos.x, worldPos.y)).sqrMagnitude;
                    if (d2 < bestDist)
                    {
                        bestDist = d2;
                        bestCell = candidate;
                    }
                }
            }

            return bestDist < float.MaxValue;
        }

        // ========== SCREEN->CELL precise mapping ==========
        public bool TryScreenToCell(Camera cam, Vector3 screenPos, out Vector3Int cell)
        {
            cell = default;
            if (cam == null) return false;

            // Ray dal mouse
            var ray = cam.ScreenPointToRay(screenPos);

            // Intersezione col piano z=0 (mondo) → t = -ray.origin.z / ray.direction.z
            if (Mathf.Approximately(ray.direction.z, 0f)) return false; // Parallelo al piano
            float t = -ray.origin.z / ray.direction.z;
            if (t < 0f) return false; // Dietro la camera

            Vector3 worldOnPlane = ray.origin + ray.direction * t; // Punto world proiettato sul piano di gioco

            // Usa snapping alla cella con centro più vicino per correggere bordi del diamante
            return TryWorldToNearestCell(worldOnPlane, out cell);
        }

        // ========== PATHFINDING SUPPORT (SPRINT 1) ==========
        
        public bool IsCellWalkable(Vector2Int cell)
        {
            // Bounds check
            if (cell.x < 0 || cell.y < 0 || cell.x >= _tileManager.Width || cell.y >= _tileManager.Height)
                return false;

            // Tile must exist
            var tile = _tileManager.GetGrid().GetValue(cell.x, cell.y);
            if (tile == null)
                return false;

            // OBSTACLES - Block pathfinding:
            // - Buildings (occupied cells)
            // - Resources (trees, stones, gold, etc.)
            // - Units, Enemies, Objects (future - tracked by their own managers)
            if (_occupiedCells.Contains(cell))
                return false; // Building occupies this cell

            var resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceManager != null && resourceManager.HasResourceAt(cell))
                return false; // Resource occupies this cell

            return true;
        }

        public List<Vector2Int> GetWalkableNeighbors(Vector2Int cell)
        {
            // 4-directional cross pattern (isometric compatible)
            var neighbors = new List<Vector2Int>(4)
            {
                new Vector2Int(cell.x + 1, cell.y),  // East
                new Vector2Int(cell.x - 1, cell.y),  // West
                new Vector2Int(cell.x, cell.y + 1),  // North
                new Vector2Int(cell.x, cell.y - 1)   // South
            };

            // Filter only walkable neighbors
            neighbors.RemoveAll(n => !IsCellWalkable(n));
            return neighbors;
        }
    }
}

