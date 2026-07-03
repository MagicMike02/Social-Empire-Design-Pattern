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

            // BUG FIX: in una griglia isometrica il mapping AABB-world → range-celle NON è
            // rettangolare. Usare solo 2 angoli opposti (min, max) dell'AABB produce un range Y
            // sistematicamente troppo stretto: alcune tile visibili non entrano mai nel loop
            // e restano disabilitate (symptom: tile mancanti a zoom alto + drag veloce).
            //
            // Proiezione iso della griglia:
            //   world.x = (gx - gy) * cs
            //   world.y = (gx + gy) * cs * 0.5
            // Invertendo:
            //   gy_min reale = (camMin.y*2 - camMax.x) / (2*cs)  → dipende da camMax.x, non camMin.x
            //   gy_max reale = (camMax.y*2 - camMin.x) / (2*cs)  → dipende da camMin.x, non camMax.x
            //
            // Fix: converti TUTTI e 4 gli angoli dell'AABB → prendi min/max sui 4 risultati.
            Vector3 bMin = camBounds.min;
            Vector3 bMax = camBounds.max;

            _gridManager.TryWorldToCell(new Vector3(bMin.x, bMin.y, 0), out Vector3Int c0);
            _gridManager.TryWorldToCell(new Vector3(bMax.x, bMin.y, 0), out Vector3Int c1); // contiene gy_min reale
            _gridManager.TryWorldToCell(new Vector3(bMin.x, bMax.y, 0), out Vector3Int c2); // contiene gy_max reale
            _gridManager.TryWorldToCell(new Vector3(bMax.x, bMax.y, 0), out Vector3Int c3);

            int xMin = Mathf.Max(0,         Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x)) - 1);
            int xMax = Mathf.Min(gridW - 1,  Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x)) + 1);
            int yMin = Mathf.Max(0,         Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y)) - 1);
            int yMax = Mathf.Min(gridH - 1,  Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y)) + 1);

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
                        bool isVisible = camBounds.Contains(tile.WorldPosition);
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
