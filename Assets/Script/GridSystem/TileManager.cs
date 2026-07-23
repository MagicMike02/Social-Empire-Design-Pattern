using UnityEngine;
using VContainer;
using Script.Common;

namespace Script.GridSystem
{
    /// <summary>
    /// Gestisce l'instanziazione visiva della griglia e dei singoli Tile.
    /// </summary>
    public class TileManager : MonoBehaviour
    {
        #region Editor Fields
        
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private float cellSize;
        
        #endregion

		

        #region Private Fields
        
        private const string DefaultShaderName = "Sprites/Default";
        
        private Grid<Tile> _grid;
        private PrefabPoolManager _poolManager;
        // Materiale condiviso tra tutti i Tile: 1 istanza invece di 10k → abilita batching.
        // Ownership centralizzata in TileManager (risolve race condition di Tile.OnDestroy static).
        private Material _sharedTileMaterial;
        
        #endregion

        #region Properties
        
        public int Width => width;
        public int Height => height;
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            var shader = Shader.Find(DefaultShaderName);
            if (shader != null)
            {
                _sharedTileMaterial = new Material(shader);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError($"[TileManager] Impossibile trovare lo shader {DefaultShaderName} per il materiale condiviso!");
#endif
            }
        }

        private void OnDestroy()
        {
            if (_sharedTileMaterial != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(_sharedTileMaterial);
                else
                    DestroyImmediate(_sharedTileMaterial);
#else
                Destroy(_sharedTileMaterial);
#endif
                _sharedTileMaterial = null;
            }
        }

        #endregion

        #region DI

        [Inject]
        public void Construct(PrefabPoolManager poolManager)
        {
            try
            {
                _poolManager = poolManager;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[TileManager] Errore durante Construct: {ex.Message}");
#endif
            }
        }
        
        #endregion

        #region Public APIs
        
        /// <summary>
        /// Crea e posiziona la griglia logica e visiva nel mondo di gioco.
        /// </summary>
        public void CreateGrid()
        {
            _grid = new Grid<Tile>(width, height, cellSize);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CreateTileAt(x, y);
                }
            }
        }

        /// <summary>
        /// Restituisce l'istanza core della griglia logica creata.
        /// </summary>
        public Grid<Tile> GetGrid() => _grid;

        /// <summary>
        /// Rilascia un singolo Tile nel pool (per future espansioni: demolizione/riciclo tile).
        /// </summary>
        public void ReleaseTile(Tile tile)
        {
            if (tile == null || _poolManager == null || _tilePrefab == null) return;

            var go = tile.gameObject;
            _poolManager.Release(_tilePrefab, go);
        }
        
        #endregion

        #region Private Helpers

        private void CreateTileAt(int x, int y)
        {
            Vector2 gridPosition = new Vector2(x, y);
            Vector3 worldPosition = _grid.GetIsoToWorldPosition(x, y);
            Tile tile = InstantiateTile(worldPosition);
            int sortingOrder = CalculateSortingOrder(x, y);
            tile.Initialize(gridPosition, worldPosition, sortingOrder, _sharedTileMaterial);
            _grid.SetValue(x, y, tile);
        }

        private Tile InstantiateTile(Vector3 position)
        {
            GameObject obj;
            
            // Preferisci il pool se disponibile (VContainer inject), fallback a Instantiate.
            if (_poolManager != null && _tilePrefab != null)
            {
                obj = _poolManager.Get(_tilePrefab, position, Quaternion.identity, transform);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("[TileManager] PrefabPoolManager non iniettato — fallback a Instantiate().");
#endif
                obj = Instantiate(_tilePrefab, position, Quaternion.identity, transform);
            }

            return obj.GetComponent<Tile>();
        }

        private int CalculateSortingOrder(int x, int y)
        {
            return (x + y) * -100;
        }

        #endregion
    }
}

