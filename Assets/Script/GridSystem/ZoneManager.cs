﻿using System.Collections.Generic;
 using Script.Core.Commands;
 using Script.Core.Events;
 using Script.EconomySystem;
 using Script.GridSystem.Commands;
 using Script.ResourceSystem.Enums;
 using VContainer;
using UnityEngine;

namespace Script.GridSystem
{
    public class ZoneManager : MonoBehaviour
    {
        #region Dependencies
        
        private GameEconomyManager _economyManager;
        private CommandHistory _commandHistory;

        [Inject]
        public void Construct(GameEconomyManager economyManager, CommandHistory commandHistory)
        {
            _economyManager = economyManager;
            _commandHistory = commandHistory;
        }
        
        #endregion

        #endregion

        #region Config & Fields
        
        [SerializeField] private GameObject _purchaseSignPrefab;
        [SerializeField] private ZoneExpansionDataSO _expansionData;
        
        private const int _zoneSize = 20;
        private Dictionary<Vector2Int, Zone> _zones = new();
        private Grid<Tile> _grid;
        public Dictionary<Vector2Int, GameObject> occupiedTiles = new();
        
        private int _unlockedZonesCount = 0;
        
        #endregion

        #region Properties

        public int ZoneSize => _zoneSize;
        
        #endregion

        #region Initialization

        /// <summary>
        /// Inizializza il manager interfacciandolo con la griglia base.
        /// </summary>
        public void Initialize(Grid<Tile> grid)
        {
            _grid = grid;
        }
        #endregion
        
        #region Public APIs

        /// <summary>
        /// Lancia un comando d'acquisto per validare le risorse e completarne la transazione da UI.
        /// </summary>
        public void PurchaseZone(Vector2Int zoneCoord, Dictionary<ResourceType, int> cost)
        {
            var command = new PurchaseZoneCommand(this, _economyManager, zoneCoord, cost);
            if (_commandHistory.ExecuteCommand(command))
            {
                GlobalEventBus.Publish(new ZoneUnlockedEvent(0, zoneCoord));
                
                // Aggiorna anche gli altri sign sulla mappa affinchè riflettano il nuovo costo
                UpdateAllPurchaseSignCosts();
            }
            else
            {
                GlobalEventBus.Publish(new ZonePurchaseFailedEvent(zoneCoord, "Purchase failed"));
            }
        }

        /// <summary>
        /// Controlla se la coordinata ha una zona istanziata.
        /// </summary>
        public bool HasZone(Vector2Int coord) => _zones.ContainsKey(coord);

        /// <summary>
        /// Restituisce lo stato sbloccato della zona data la sua coordinata.
        /// </summary>
        public bool IsZoneUnlocked(Vector2Int coord) => 
            _zones.TryGetValue(coord, out var zone) && zone.isUnlocked;

        /// <summary>
        /// Aggiorna a livello di root tutte le tile della zona sbloccandole e rimuovendo il sign.
        /// </summary>
        public void UnlockZone(Vector2Int coord)
        {
            if (!_zones.TryGetValue(coord, out var zone)) return;

            zone.isUnlocked = true;
            _unlockedZonesCount++;
            
            foreach (var tile in zone.tiles)
                if (tile) tile.Unlock();

            if (zone.purchaseSign)
            {
                Vector2Int signGridPos = zone.start + new Vector2Int(_zoneSize / 2, _zoneSize / 2);
                occupiedTiles.Remove(signGridPos);
                Destroy(zone.purchaseSign);
            }
        }

        /// <summary>
        /// Ri-blocca la zona, ricreando il suo Sign d'acquisto.
        /// </summary>
        public void LockZone(Vector2Int coord)
        {
            if (!_zones.TryGetValue(coord, out var zone)) return;

            zone.isUnlocked = false;
            _unlockedZonesCount--;
            
            foreach (var tile in zone.tiles)
                if (tile) tile.SetState(TileState.Locked);

            CreatePurchaseSign(zone);
        }

        /// <summary>
        /// Popola la mappa con le macro-aree quadrate delle zone e imposta il punto centrale di partenza.
        /// </summary>
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
                        _unlockedZonesCount++;
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

        #endregion

        #region Internal Utilities

        private void CreatePurchaseSign(Zone zone)
        {
            Vector2Int centerTilePos = zone.start + new Vector2Int(_zoneSize / 2, _zoneSize / 2);
            Vector3 worldPos = _grid.GetIsoToWorldPosition(centerTilePos.x, centerTilePos.y);
            
            Vector3 signOffset = new Vector3(0, 0.45f, 0);
            Vector3 signPosition = worldPos + signOffset;
            
            GameObject signObj = Instantiate(_purchaseSignPrefab, signPosition, Quaternion.identity, transform);
            
            occupiedTiles.Add(centerTilePos, signObj);
           
            var sign = signObj.GetComponent<PurchaseSign>();
            var cost = _expansionData != null ? _expansionData.GetCostForNextZone(_unlockedZonesCount) : new Dictionary<ResourceType, int>();
            
            sign.Setup(zone.start, _economyManager, this, cost);
            zone.purchaseSign = signObj;
            
            foreach (var tile in zone.tiles)
            {
                if (tile != null) tile.SetState(TileState.Buyable);
            }
        }
        
        private void UpdateAllPurchaseSignCosts()
        {
            var nextCost = _expansionData != null ? _expansionData.GetCostForNextZone(_unlockedZonesCount) : new Dictionary<ResourceType, int>();

            foreach (var zone in _zones.Values)
            {
                if (!zone.isUnlocked && zone.purchaseSign != null)
                {
                    var sign = zone.purchaseSign.GetComponent<PurchaseSign>();
                    if (sign != null)
                    {
                        sign.Setup(zone.start, _economyManager, this, nextCost);
                    }
                }
            }
        }
        
        #endregion

        #region Inner Classes

        /// <summary>
        /// Container interno che rappresenta la logica di array per le celle all'interno della zona.
        /// </summary>
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
        
        #endregion
    }
}
