using System;
using Script.BuildingSystem;
using Script.GridSystem;
using Script.ResourceSystem;
using UnityEngine;
using VContainer;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Script.Core.Optimization
{
    /// <summary>
    /// Ottimizza il rendering disabilitando i SpriteRenderer di Tile, Risorse ed Edifici
    /// che si trovano al di fuori del viewport della telecamera principale.
    /// </summary>
    public class GridCullingManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Tempo in secondi tra un controllo di culling e l'altro.")]
        [SerializeField] private float _cullingInterval = 0.5f;
        [Tooltip("Margine extra (in unità world) oltre il bordo della telecamera per evitare pop-in visivi.")]
        [SerializeField] private float _overscanPadding = 5f;

        private Camera _mainCamera;
        private GridManager _gridManager;
        private TileManager _tileManager;
        private CancellationTokenSource _cullingCts;

        private Vector3 _lastCameraPos;
        private float _lastCameraOrthoSize;

        [Inject]
        public void Construct(Camera mainCamera, GridManager gridManager, TileManager tileManager)
        {
            _mainCamera = mainCamera;
            _gridManager = gridManager;
            _tileManager = tileManager;
        }

        // Cache per evitare GetComponentsInChildren ad ogni tick
        private readonly Dictionary<EntityId, Renderer[]> _rendererCache = new();

        private void OnEnable()
        {
            if (_mainCamera != null && _gridManager != null && _tileManager != null)
            {
                _cullingCts?.Cancel();
                _cullingCts?.Dispose();
                _cullingCts = new CancellationTokenSource();
                _ = RunCullingLoopAsync(_cullingCts.Token);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("[GridCullingManager] Dipendenze mancanti. Assicurati che VContainer le abbia iniettate.");
#endif
            }
        }

        private void OnDisable()
        {
            if (_cullingCts != null)
            {
                _cullingCts.Cancel();
                _cullingCts.Dispose();
                _cullingCts = null;
            }
            
            _rendererCache.Clear();
        }

        private async Task RunCullingLoopAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1f), token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                PerformCulling();
                UpdateCameraState();

                while (!token.IsCancellationRequested)
                {
                    if (CameraHasChanged())
                    {
                        PerformCulling();
                        UpdateCameraState();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.05f, _cullingInterval)), token);
                }
            }
            catch (TaskCanceledException)
            {
                // Atteso durante OnDisable
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[GridCullingManager] Errore nel loop di culling: {ex.Message}\n{ex.StackTrace}");
#endif
            }
        }

        private bool CameraHasChanged()
        {
            if (Vector3.SqrMagnitude(_mainCamera.transform.position - _lastCameraPos) > 0.1f) return true;
            if (Mathf.Abs(_mainCamera.orthographicSize - _lastCameraOrthoSize) > 0.1f) return true;
            return false;
        }

        private void UpdateCameraState()
        {
            _lastCameraPos = _mainCamera.transform.position;
            _lastCameraOrthoSize = _mainCamera.orthographicSize;
        }

        private void PerformCulling()
        {
            if (_tileManager.GetGrid() == null) return;

            // 1. Calcola il bounding box (AABB) espanso della telecamera
            Bounds camBounds = CalculateExpandedCameraBounds();

            // 2. Culling sui Tile — query spaziale invece di O(W×H) full scan.
            //    Converte i 4 angoli del viewport in coordinate griglia e itera solo
            //    sulle celle potenzialmente visibili (tipicamente << 100×100).
            CullTilesInRange(camBounds);

            // 3. Culling su Entity occupanti (Edifici, Risorse, Cartelli)
            var occupancy = _gridManager.GetOccupancySnapshot();
            foreach (var kvp in occupancy)
            {
                GameObject entityObj = kvp.Value;
                if (entityObj != null)
                {
                    Vector3 worldPos = entityObj.transform.position;
                    // Manteniamo visibili gli oggetti grossi allargando ancor di più il controllo se necessario,
                    // ma l'overscan padding lo copre già nella maggior parte dei casi.
                    bool isVisible = camBounds.Contains(worldPos);
                    
                    // Spegniamo solo i renderer, non i collider o script
                    ToggleRenderers(entityObj, isVisible);
                }
            }
        }

        /// <summary>
        /// Itera solo sulle celle che intersecano i bounds della camera (range query).
        /// Su griglia 100×100 con camera che ne copre ~20×20, riduce le iterazioni da 10k a ~400.
        /// </summary>
        private void CullTilesInRange(Bounds camBounds)
        {
            var grid = _tileManager.GetGrid();
            int gridW = _tileManager.Width;
            int gridH = _tileManager.Height;

            // Converte i 4 angoli del bounds in coordinate griglia.
            // Per griglie isometriche usiamo TryWorldToCell che gestisce la matrice iso.
            Vector3 min = camBounds.min;
            Vector3 max = camBounds.max;

            // Calcola il range di celle da controllare (con clamp ai bounds della griglia).
            _gridManager.TryWorldToCell(min, out Vector3Int minCell);
            _gridManager.TryWorldToCell(max, out Vector3Int maxCell);

            // Se la camera è completamente fuori griglia, TryWorldToCell ritorna false
            // ma i valori possono essere negativi/oob — clampiamo a [0, W/H].
            int xMin = Mathf.Max(0, Mathf.Min(minCell.x, maxCell.x) - 1);
            int xMax = Mathf.Min(gridW - 1, Mathf.Max(minCell.x, maxCell.x) + 1);
            int yMin = Mathf.Max(0, Mathf.Min(minCell.y, maxCell.y) - 1);
            int yMax = Mathf.Min(gridH - 1, Mathf.Max(minCell.y, maxCell.y) + 1);

            // Edge case: range invalido (camera fuori griglia) → disabilita tutto.
            if (xMin > xMax || yMin > yMax)
            {
                // Fallback: full scan per sicurezza (raro, solo se camera è fuori bounds).
                for (int x = 0; x < gridW; x++)
                {
                    for (int y = 0; y < gridH; y++)
                    {
                        Tile tile = grid.GetValue(x, y);
                        if (tile != null) ToggleRenderers(tile.gameObject, false);
                    }
                }
                return;
            }

            // Itera solo sul sub-range visibile.
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    Tile tile = grid.GetValue(x, y);
                    if (tile != null)
                    {
                        Vector3 worldPos = _gridManager.CellToWorld(new Vector3Int(x, y, 0));
                        bool isVisible = camBounds.Contains(worldPos);
                        ToggleRenderers(tile.gameObject, isVisible);
                    }
                }
            }
        }

        private Bounds CalculateExpandedCameraBounds()
        {
            float orthoSize = _mainCamera.orthographicSize;
            float aspect = _mainCamera.aspect;

            float height = orthoSize * 2f;
            float width = height * aspect;

            Vector3 center = _mainCamera.transform.position;
            center.z = 0; // Schiaccia sul piano 2D

            Vector3 size = new Vector3(width + _overscanPadding * 2f, height + _overscanPadding * 2f, 100f);

            return new Bounds(center, size);
        }

        /// <summary>
        /// Ottimizza l'attivazione/disattivazione controllando lo stato precedente, 
        /// recuperando il buffer dalla cache per non pesare sul Garbage Collector.
        /// </summary>
        private void ToggleRenderers(GameObject obj, bool isVisible)
        {
            EntityId entityId = obj.GetEntityId();
            
            if (!_rendererCache.TryGetValue(entityId, out Renderer[] renderers))
            {
                // Unico GetComponents al primo incontro
                renderers = obj.GetComponentsInChildren<Renderer>(true);
                _rendererCache[entityId] = renderers;
            }

            // Evita allocazioni future
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled != isVisible)
                {
                    renderers[i].enabled = isVisible;
                }
            }
        }
    }
}
