using UnityEngine;

namespace Script2.GridSystem
{
    public class TileManager : MonoBehaviour
    {
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private float cellSize;
        private Grid<Tile> _grid;

        public int Width => width;
        public int Height => height;
        
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

        public Grid<Tile> GetGrid() => _grid;
    }
}

