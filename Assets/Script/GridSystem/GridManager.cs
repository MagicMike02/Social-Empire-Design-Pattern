using UnityEngine;
using System.Collections.Generic;
using Script.BuildingSystem;
using Script.Core.Events;
using VContainer;

namespace Script.GridSystem
{
    /// <summary>
    /// Gestisce la griglia di gioco, integrando TileManager e ZoneManager.
    /// Implementa IGridService per fornire operazioni sulla griglia al BuildingSystem.
    /// </summary>
    public class GridManager : MonoBehaviour, IGridService
    {
        #region Dependencies (Injected by VContainer)
        
        private TileManager _tileManager;
        private ZoneManager _zoneManager;

        [Inject]
        public void Construct(
            TileManager tileManager,
            ZoneManager zoneManager)
        {
            _tileManager = tileManager;
            _zoneManager = zoneManager;
        }
        
        #endregion

        #region Private Fields
        
        private readonly List<Vector2Int> _lastPreviewCells = new();
        private readonly Dictionary<Vector2Int, Tile> _previewTileCache = new();
        private readonly Dictionary<Vector2Int, GameObject> _occupiedCells = new();
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateDependencies();
            InitializeGrid();
        }
        
        #endregion

        #region Initialization

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
        
        #endregion

        #region Layout Properties

        public int Width  => _tileManager.Width;
        public int Height => _tileManager.Height;
        
        #endregion

        #region Core Grid Queries (IGridService)

        /// <summary>
        /// Converte una posizione nel mondo fisico in una coordinata cella sulla griglia.
        /// </summary>
        public bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell)
        {
            var grid = _tileManager.GetGrid();
            grid.GetWorldToIsoPosition(worldPos, out int x, out int y);
            cell = new Vector3Int(x, y, 0);

            bool inside = x >= 0 && y >= 0 && x < _tileManager.Width && y < _tileManager.Height;
            
            return inside;
        }

        /// <summary>
        /// Ottiene il centro fisico (World Position) corrispondente a una determinata cella logica.
        /// </summary>
        public Vector3 CellToWorld(Vector3Int cell)
        {
            var grid = _tileManager.GetGrid();
            return grid.GetIsoToWorldPosition(cell.x, cell.y);
        }

        /// <summary>
        /// Verifica se un blocco rettangolare di celle e' completamente libero (state Unlocked e non occupato).
        /// </summary>
        public bool AreCellsFree(Vector3Int originCell, int width, int height)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int x = originCell.x + dx;
                    int y = originCell.y + dy;
                    
                    // Bounds check
                    if (x < 0 || y < 0 || x >= _tileManager.Width || y >= _tileManager.Height) 
                        return false;

                    var p = new Vector2Int(x, y);
                    
                    // Check occupancy (unica fonte di verità)
                    if (_occupiedCells.ContainsKey(p)) 
                        return false;

                    // Check tile state
                    var tile = _tileManager.GetGrid().GetValue(x, y);
                    if (tile == null || tile.State != TileState.Unlocked) 
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Segna come occupate le celle specificate, associandole a un edificio e pubblicando CellsOccupiedEvent.
        /// </summary>
        public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var p = new Vector2Int(originCell.x + dx, originCell.y + dy);
                    _occupiedCells[p] = building.gameObject;
                }
            }
            
            // Pubblica evento GlobalEventBus per notificare occupazione celle
            GlobalEventBus.Publish(new CellsOccupiedEvent(
                originCell, 
                width, 
                height, 
                building?.gameObject
            ));
        }

        /// <summary>
        /// Libera un blocco di celle occupate e pubblica CellsFreedEvent.
        /// </summary>
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
            
            // Pubblica evento GlobalEventBus per notificare liberazione celle
            GlobalEventBus.Publish(new CellsFreedEvent(originCell, width, height));
        }

        /// <summary>
        /// Applica una visuale di preview per un blocco di celle (es. verde per valido, rosso per invalido).
        /// </summary>
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
        
        /// <summary>
        /// Rimuove la tint di preview dalle celle.
        /// </summary>
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

        #endregion

        #region Complex Mapping

        /// <summary>
        /// Helper: trova la cella il cui centro logico è più vicino a worldPos, valutando i 9 vicini.
        /// </summary>
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

        /// <summary>
        /// Esegue un raycast dalla telecamera per convertire un punto sullo schermo nella cella d'intersezione sul piano orizzontale.
        /// </summary>
        public bool TryScreenToCell(Camera cam, Vector3 screenPos, out Vector3Int cell)
        {
            cell = default;
            if (cam == null) return false;

            // Ray dal mouse
            var ray = cam.ScreenPointToRay(screenPos);

            if (Mathf.Approximately(ray.direction.z, 0f)) return false;
            float t = -ray.origin.z / ray.direction.z;
            if (t < 0f) return false; // Dietro la camera

            Vector3 worldOnPlane = ray.origin + ray.direction * t; // Punto world proiettato sul piano di gioco

            // Usa snapping alla cella con centro più vicino per correggere bordi del diamante
            return TryWorldToNearestCell(worldOnPlane, out cell);
        }

        #endregion

        #region Pathfinding Support
        
        /// <summary>
        /// Verifica se una cella è attraversabile da unità (non bloccata da ostacoli o zone inesplorate).
        /// </summary>
        public bool IsCellWalkable(Vector2Int cell)
        {
            // Bounds check
            if (cell.x < 0 || cell.y < 0 || cell.x >= _tileManager.Width || cell.y >= _tileManager.Height)
                return false;

            // Tile must exist
            var tile = _tileManager.GetGrid().GetValue(cell.x, cell.y);
            if (tile == null)
                return false;

            // Check occupancy (buildings + resources + signs already in _occupiedCells)
            if (_occupiedCells.ContainsKey(cell))
                return false;

            return true;
        }

        /// <summary>
        /// Esegue query sulla griglia per estrarre tutti i vicini attraversabili (8-way).
        /// </summary>
        public List<Vector2Int> GetWalkableNeighbors(Vector2Int cell)
        {
            // OPTIMIZED: 8-directional movement (4 cardinal + 4 diagonal)
            // Allocate max capacity (8 neighbors)
            var neighbors = new List<Vector2Int>(8);
            
            // ===== CARDINAL DIRECTIONS (cost = 1) =====
            Vector2Int east = new Vector2Int(cell.x + 1, cell.y);
            if (IsValidCell(east) && IsCellWalkable(east))
                neighbors.Add(east);
            
            Vector2Int west = new Vector2Int(cell.x - 1, cell.y);
            if (IsValidCell(west) && IsCellWalkable(west))
                neighbors.Add(west);
            
            Vector2Int north = new Vector2Int(cell.x, cell.y + 1);
            if (IsValidCell(north) && IsCellWalkable(north))
                neighbors.Add(north);
            
            Vector2Int south = new Vector2Int(cell.x, cell.y - 1);
            if (IsValidCell(south) && IsCellWalkable(south))
                neighbors.Add(south);
            
            // ===== DIAGONAL DIRECTIONS (cost = sqrt(2) ≈ 1.414) =====
            // Only allow diagonal if BOTH adjacent cardinals are walkable (prevent corner-cutting)
            Vector2Int northEast = new Vector2Int(cell.x + 1, cell.y + 1);
            if (IsValidCell(northEast) && IsCellWalkable(northEast) && IsCellWalkable(east) && IsCellWalkable(north))
                neighbors.Add(northEast);
            
            Vector2Int northWest = new Vector2Int(cell.x - 1, cell.y + 1);
            if (IsValidCell(northWest) && IsCellWalkable(northWest) && IsCellWalkable(west) && IsCellWalkable(north))
                neighbors.Add(northWest);
            
            Vector2Int southEast = new Vector2Int(cell.x + 1, cell.y - 1);
            if (IsValidCell(southEast) && IsCellWalkable(southEast) && IsCellWalkable(east) && IsCellWalkable(south))
                neighbors.Add(southEast);
            
            Vector2Int southWest = new Vector2Int(cell.x - 1, cell.y - 1);
            if (IsValidCell(southWest) && IsCellWalkable(southWest) && IsCellWalkable(west) && IsCellWalkable(south))
                neighbors.Add(southWest);
            
            return neighbors;
        }

        /// <summary>
        /// Verifica se una cella è all'interno dei limiti della griglia.
        /// Fast bounds check before IsCellWalkable (avoids out-of-bounds access).
        /// </summary>
        private bool IsValidCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < _tileManager.Width && 
                   cell.y >= 0 && cell.y < _tileManager.Height;
        }

        #region Public Occupancy Methods (per ResourceManager, ZoneManager)

        /// <summary>
        /// Occupa una singola cella (risorse, sign, etc).
        /// </summary>
        public void OccupyCell(Vector2Int cell, GameObject context)
        {
            _occupiedCells[cell] = context;
        }

        /// <summary>
        /// Libera una singola cella.
        /// </summary>
        public void FreeCell(Vector2Int cell)
        {
            _occupiedCells.Remove(cell);
        }

        /// <summary>
        /// Verifica se una singola cella è libera.
        /// </summary>
        public bool IsCellFree(Vector2Int cell)
        {
            return !_occupiedCells.ContainsKey(cell);
        }

        /// <summary>
        /// Snapshot di tutte le occupanze (utility per save game, debugging).
        /// </summary>
        public IReadOnlyDictionary<Vector2Int, GameObject> GetOccupancySnapshot()
        {
            return new Dictionary<Vector2Int, GameObject>(_occupiedCells);
        }

        #endregion
    }
}

