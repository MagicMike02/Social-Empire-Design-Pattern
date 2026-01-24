using System;
using System.Collections.Generic;
using Script2.Economy;
using Script2.ResourceSystem.Enums;
using UnityEngine;

namespace Script2.GridSystem
{
    public class ZoneManager : MonoBehaviour
    {
        [SerializeField] private GameEconomyManager _economyManager;
        [SerializeField] private GameObject _purchaseSignPrefab;
        [SerializeField] private int _zoneSize = 20;

        private Dictionary<Vector2Int, Zone> _zones = new();
        private Dictionary<ResourceType, int> _zoneCost = new() { { ResourceType.Gold, 10 } };
        private Grid<Tile> _grid;
        public Dictionary<Vector2Int, GameObject> occupiedTiles = new();

        public event Action<Vector2Int> OnZoneUnlocked;
        public event Action<Vector2Int> OnZonePurchaseFailed;

        public int ZoneSize => _zoneSize;

        public void Initialize(Grid<Tile> grid)
        {
            _grid = grid;
        }

        public void CreateZones(int width, int height)
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
                                zone.tiles[dx, dy] = _grid.GetValue(gx, gy);
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

        public void PurchaseZone(Vector2Int zoneCoord)
        {
            if (!_zones.TryGetValue(zoneCoord, out var zone) || zone.isUnlocked) return;
            if (_economyManager && _economyManager.CanAfford(_zoneCost))
            {
                _economyManager.SpendResources(_zoneCost);
                zone.isUnlocked = true;
                foreach (var tile in zone.tiles)
                {
                    // if (tile) tile.SetState(TileState.Unlocked);
                    if (tile) tile.Unlock();
                }
                if (zone.purchaseSign) 
                {
                    Vector2Int signGridPos = zone.start + new Vector2Int(_zoneSize / 2, _zoneSize / 2);
                    occupiedTiles.Remove(signGridPos);
                    Destroy(zone.purchaseSign);
                }
                Debug.Log($"Zona sbloccata in {zoneCoord}");
                OnZoneUnlocked?.Invoke(zoneCoord);
            }
            else if (_economyManager)
            {
                Debug.Log("Non hai abbastanza risorse per sbloccare questa zona!");
                OnZonePurchaseFailed?.Invoke(zoneCoord);
            }
            else
            {
                Debug.LogError("GameEconomyManager instance not found. Cannot check/spend resources.");
            }
        }

        private void CreatePurchaseSign(Zone zone)
        {
            Vector2Int centerTilePos = zone.start + new Vector2Int(_zoneSize / 2, _zoneSize / 2);
            Vector3 worldPos = _grid.GetIsoToWorldPosition(centerTilePos.x, centerTilePos.y);
            
            Vector3 signOffset = new Vector3(0, 0.45f, 0);
            Vector3 signPosition = worldPos + signOffset;
            
            GameObject signObj = Instantiate(_purchaseSignPrefab, signPosition, Quaternion.identity, transform);
            
            occupiedTiles.Add(centerTilePos, signObj);
           
            var sign = signObj.GetComponent<PurchaseSign>();
            sign.Setup(zone.start, _economyManager, this, new() { { ResourceType.Gold, 15 } });
            zone.purchaseSign = signObj;
            
            foreach (var tile in zone.tiles)
            {
                if (tile != null) tile.SetState(TileState.Buyable);
            }
        }

        public class Zone
        {
            public Vector2Int start;
            public Tile[,] tiles;
            public bool isUnlocked;
            public GameObject purchaseSign;
            public Zone(Vector2Int start, int size)
            {
                this.start = start;
                tiles = new Tile[size, size];
            }
        }
    }
}
