using System.Collections.Generic;
using System.Resources;
using Script2.Economy;
using Script2.ResourceSystem.Enums;
using UnityEngine;

namespace Script2.GridSystem
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        [SerializeField] public int width;
        [SerializeField] public int height;
        [SerializeField] private float cellSize;

        [SerializeField] private GameObject _tilePrefab;

        private Grid<Tile> _grid;
        public  Dictionary<Vector2Int, GameObject> occupiedTiles = new();
        
        private Dictionary<Vector2Int, Zone> _zones = new();

        private Dictionary<ResourceType, int> zoneCost = new()
        {
            { ResourceType.Gold, 10 }
        };
        private int _zoneSize { get; } = 20;
        [SerializeField] private GameObject _purchaseSignPrefab;


        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Mantiene tra le scene, se necessario
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            //Initialize Grid
            CreateGrid();
            CreateZones();
        }

        void CreateGrid()
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

        void CreateZones()
        {
            for (int x = 0; x < width; x += _zoneSize)
            {
                for (int y = 0; y < height; y += _zoneSize)
                {
                    Vector2Int zoneCoord = new(x, y);
                    Zone zone = new Zone(zoneCoord, _zoneSize);

                    for (int dx = 0; dx < _zoneSize; dx++)
                    {
                        for (int dy = 0; dy < _zoneSize; dy++)
                        {
                            int gx = x + dx;
                            int gy = y + dy;

                            if (gx < width && gy < height)
                            {
                                zone.tiles[dx, dy] = _grid.GetValue(gx, gy);
                            }
                        }
                    }

                    Vector2Int centerCoord = new(width / 2 - _zoneSize / 2, height / 2 - _zoneSize / 2);

                    if (zoneCoord == centerCoord)
                    {
                        zone.isUnlocked = true;
                        for (int dx = 0; dx < _zoneSize; dx++)
                        {
                            for (int dy = 0; dy < _zoneSize; dy++)
                            {
                                if (zone.tiles[dx, dy] != null)
                                    zone.tiles[dx, dy].SetState(TileState.Unlocked);
                            }
                        }
                    }
                    else if (Mathf.Abs(zoneCoord.x - centerCoord.x) <= _zoneSize &&
                             Mathf.Abs(zoneCoord.y - centerCoord.y) <= _zoneSize)
                    {
                        CreatePurchaseSign(zone);
                    }

                    _zones[zoneCoord] = zone;
                }
            }
        }

        void CreateTileAt(int x, int y)
        {
            Vector2 gridPosition = new Vector2(x, y);
            Vector3 worldPosition = _grid.GetIsoToWorldPosition(x, y);

            Tile tile = InstantiateTile(worldPosition);

            int sortingOrder = CalculateSortingOrder(x, y);
            tile.Initialize(gridPosition, worldPosition, sortingOrder);

            //AssignTileToSector(tile, x, y);

            _grid.SetValue(x, y, tile);
        }

        Tile InstantiateTile(Vector3 position)
        {
            // Istanzia e assegna direttamente il parent
            GameObject obj = Instantiate(_tilePrefab, position, Quaternion.identity, transform);
            return obj.GetComponent<Tile>();
        }

        int CalculateSortingOrder(int x, int y)
        {
            return (x + y) * -100;
        }


        public void PurchaseZone(Vector2Int zoneCoord)
        {
            if (_zones.TryGetValue(zoneCoord, out Zone zone) && !zone.isUnlocked)
            {
                if (GameEconomyManager.Instance != null && GameEconomyManager.Instance.CanAfford(zoneCost))
                {
                    GameEconomyManager.Instance.SpendResources(zoneCost);
                    zone.isUnlocked = true;
                    foreach (var tile in zone.tiles)
                    {
                        if (tile != null) { tile.SetState(TileState.Unlocked); }
                    }
                    if (zone.purchaseSign != null)
                    {
                        Vector2Int signGridPos = zone.start + new Vector2Int(_zoneSize / 2, _zoneSize / 2);
                        occupiedTiles.Remove(signGridPos);
                        Destroy(zone.purchaseSign);
                    }
                    Debug.Log($"Zona sbloccata in {zoneCoord}");
                }
                else if (GameEconomyManager.Instance != null)
                {
                    Debug.Log("Non hai abbastanza risorse per sbloccare questa zona!");
                }
                else
                {
                    Debug.LogError("GameEconomyManager instance not found. Cannot check/spend resources.");
                }
            }
        }

        void CreatePurchaseSign(Zone zone)
        {
            Vector2Int centerTilePos = zone.start + new Vector2Int(_zoneSize / 2, _zoneSize / 2);
            Vector3 worldPos = _grid.GetIsoToWorldPosition(centerTilePos.x, centerTilePos.y);
            GameObject signObj = Instantiate(_purchaseSignPrefab, worldPos + new Vector3(0,0.45f,0), Quaternion.identity, transform);

            occupiedTiles.Add(centerTilePos, signObj);
            
            var sign = signObj.GetComponent<PurchaseSign>();
            
            sign.Initialize(zone.start);
            sign.SetCost(new(){ {ResourceType.Gold, 15}} );
            
            zone.purchaseSign = signObj;

            //unlock all tile in zone
            foreach (var tile in zone.tiles)
            {
                if (tile != null) tile.SetState(TileState.Locked);
            }
        }
        
        public Tile GetTile(int x, int y)
        {
            return _grid.GetValue(x, y);
        }

        public Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            _grid.GetWorldToIsoPosition(worldPosition, out int x, out int y);
            return new Vector3Int(x, y, 0);
        }
        
        public int GetZoneSize()
        {
            return _zoneSize;
        }

        public Grid<Tile> GetGrid()
        {
            return _grid;
        }
    }


    public class Grid<T>
    {
        private int _width;
        private int _height;
        private float _cellSize;

        private Vector3 _originPosition;

        private T[,] _gridT;

        private Matrix4x4 _matrix;

        public Grid(int width, int height, float cellSize)
        {
            _width = width;
            _height = height;
            _cellSize = cellSize;
            this._gridT = new T[width, height];

            _originPosition = new Vector3(0, 0, 1);

            InitMatrix4x4();
        }

        private void InitMatrix4x4()
        {
            _matrix = Matrix4x4.identity;
            _matrix.SetColumn(0, new Vector4(1f, 0.5f, 0f, 0f)); // Colonna per x
            _matrix.SetColumn(1, new Vector4(-1f, 0.5f, 0f, 0f)); // Colonna per y
            _matrix.SetColumn(2, new Vector4(0f, 0f, 1f, 0f)); // Colonna per z  
        }

        private void ShowDebugLines()
        {
            for (int x = 0; x < _gridT.GetLength(0); x++)
            {
                for (int y = 0; y < _gridT.GetLength(1); y++)
                {
                    Vector3 worldPos = GetIsoToWorldPosition(x, y);
                    Vector3 worldPosX = GetIsoToWorldPosition(x + 1, y);
                    Vector3 worldPosY = GetIsoToWorldPosition(x, y + 1);

                    // piccolo offset Z per rendere le linee visibili sopra i tile
                    worldPos.z -= 0.1f;
                    worldPosX.z -= 0.1f;
                    worldPosY.z -= 0.1f;

                    Debug.DrawLine(worldPos, worldPosX, Color.black, 100f);
                    Debug.DrawLine(worldPos, worldPosY, Color.black, 100f);
                }
            }

            // Disegna le linee di bordo
            Vector3 topLeft = GetIsoToWorldPosition(0, _height);
            Vector3 topRight = GetIsoToWorldPosition(_width, _height);
            Vector3 bottomRight = GetIsoToWorldPosition(_width, 0);

            topLeft.z -= 0.1f;
            topRight.z -= 0.1f;
            bottomRight.z -= 0.1f;

            Debug.DrawLine(topLeft, topRight, Color.black, 100f);
            Debug.DrawLine(topRight, bottomRight, Color.black, 100f);
        }

        #region Grid

        public Vector3 GetIsoToWorldPosition(int x, int y)
        {
            Vector3 cartesianPos = new Vector3(x, y) * _cellSize;
            Vector3 isoPos = _matrix.MultiplyPoint3x4(cartesianPos);
            return new Vector3(isoPos.x, isoPos.y, 0) + _originPosition;
        }

        public void GetWorldToIsoPosition(Vector3 worldPosition, out int x, out int y)
        {
            Vector3 localPos = worldPosition - _originPosition;

            Vector3 cartesianPos = _matrix.inverse.MultiplyPoint3x4(localPos);
            x = Mathf.FloorToInt(cartesianPos.x / _cellSize);
            y = Mathf.FloorToInt(cartesianPos.y / _cellSize);
        }

        public void SetValue(int x, int y, T value)
        {
            if (x >= 0 && y >= 0 && x < _width && y < _height)
            {
                _gridT[x, y] = value;
            }
        }

        public void SetValue(Vector3 worldPosition, T value)
        {
            GetWorldToIsoPosition(worldPosition, out int x, out int y);
            SetValue(x, y, value);
        }

        public T GetValue(int x, int y)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                return _gridT[x, y];
            }

            return default(T);
        }

        public T GetValue(Vector3 worldPosition)
        {
            GetWorldToIsoPosition(worldPosition, out int x, out int y);
            return GetValue(x, y);
        }

        public Vector3 SnapWorldToGridWorld(Vector3 worldPosition)
        {
            GetWorldToIsoPosition(worldPosition, out int x, out int y);
            return GetIsoToWorldPosition(x, y);
        }
       
        public List<T> ToList()
        {
            List<T> listT = new();
            foreach (var tile in _gridT)
            {
                listT.Add(tile);
            }

            return listT;
        }

        #endregion
    }

    class Zone
    {
        public Vector2Int start; // angolo in basso a sinistra
        public Tile[,] tiles;
        public bool isUnlocked = false;
        public GameObject purchaseSign;

        public Zone(Vector2Int start, int size)
        {
            this.start = start;
            tiles = new Tile[size, size];
        }
    }
}