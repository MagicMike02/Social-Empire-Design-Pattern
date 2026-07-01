using System.Collections;
using Script.BuildingSystem;
using Script.GridSystem;
using Script.ResourceSystem;
using UnityEngine;
using VContainer;
using System.Collections.Generic;

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
        private Coroutine _cullingRoutine;

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
                _cullingRoutine = StartCoroutine(CullingRoutine());
            }
            else
            {
                Debug.LogWarning("[GridCullingManager] Dipendenze mancanti. Assicurati che VContainer le abbia iniettate.");
            }
        }

        private void OnDisable()
        {
            if (_cullingRoutine != null)
            {
                StopCoroutine(_cullingRoutine);
            }
            
            _rendererCache.Clear();
        }

        private IEnumerator CullingRoutine()
        {
            // Attendi che la griglia sia materialmente generata
            yield return new WaitForSeconds(1f);

            while (true)
            {
                // Esegui il culling solo se la camera si è mossa o ha fatto zoom
                if (CameraHasChanged())
                {
                    PerformCulling();
                    UpdateCameraState();
                }

                yield return new WaitForSeconds(_cullingInterval);
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

            // 2. Culling sui Tile
            var grid = _tileManager.GetGrid();
            for (int x = 0; x < _tileManager.Width; x++)
            {
                for (int y = 0; y < _tileManager.Height; y++)
                {
                    Tile tile = grid.GetValue(x, y);
                    if (tile != null)
                    {
                        // Usa la posizione in mondo del tile
                        Vector3 worldPos = _gridManager.CellToWorld(new Vector3Int(x, y, 0));
                        bool isVisible = camBounds.Contains(worldPos);
                        ToggleRenderers(tile.gameObject, isVisible);
                    }
                }
            }

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
