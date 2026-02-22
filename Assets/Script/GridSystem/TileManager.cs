using UnityEngine;

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
        
        private Grid<Tile> _grid;
        
        #endregion

        #region Properties
        
        public int Width => width;
        public int Height => height;
        
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
        
        #endregion

        #region Private Helpers

        private void CreateTileAt(int x, int y)
        {
            Vector2 gridPosition = new Vector2(x, y);
            Vector3 worldPosition = _grid.GetIsoToWorldPosition(x, y);
            Tile tile = InstantiateTile(worldPosition);
            int sortingOrder = CalculateSortingOrder(x, y);
            tile.Initialize(gridPosition, worldPosition, sortingOrder);
            _grid.SetValue(x, y, tile);
        }

        private Tile InstantiateTile(Vector3 position)
        {
            GameObject obj = Instantiate(_tilePrefab, position, Quaternion.identity, transform);
            return obj.GetComponent<Tile>();
        }

        private int CalculateSortingOrder(int x, int y)
        {
            return (x + y) * -100;
        }

        #endregion
    }
}

