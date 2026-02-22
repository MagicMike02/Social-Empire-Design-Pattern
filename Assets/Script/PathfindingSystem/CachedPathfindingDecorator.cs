using System;
using System.Collections.Generic;
using System.Linq;
using Script.BuildingSystem;
using UnityEngine;

namespace Script.PathfindingSystem
{
    /// <summary>
    /// Decorator Pattern: Aggiunge caching a qualsiasi algoritmo di pathfinding.
    /// Wrappa IPathfindingAlgorithm e intercetta le chiamate per usare la cache.
    /// LRU eviction quando cache supera MaxCacheSize.
    /// </summary>
    public class CachedPathfindingDecorator : IPathfindingAlgorithm
    {
        #region Private Fields
        
        private readonly IPathfindingAlgorithm _innerAlgorithm;
        private readonly Dictionary<(Vector2Int, Vector2Int), List<Vector2Int>> _cache = new();
        private const int MaxCacheSize = 500;  // ✅ Increased from 100 (less evictions)
        
        #endregion

        #region Initialization

        /// <summary>
        /// Inizializza il decorator passandovi un pathfinding interno.
        /// </summary>
        public CachedPathfindingDecorator(IPathfindingAlgorithm algorithm)
        {
            _innerAlgorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        }
        
        #endregion

        #region Public APIs

        /// <summary>
        /// Richiama il pathfinding o restituisce il percorso salvato in cache.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, IGridService gridService)
        {
            var cacheKey = (start, goal);

            // Cache hit
            if (_cache.TryGetValue(cacheKey, out var cachedPath))
            {
                #if UNITY_EDITOR
                Debug.Log($"[CachedPathfinding] Cache HIT: {start} → {goal}");
                #endif
                return new List<Vector2Int>(cachedPath); // Return copy
            }

            // Cache miss - compute path
            var path = _innerAlgorithm.FindPath(start, goal, gridService);

            // Cache result
            if (_cache.Count >= MaxCacheSize)
            {
                var firstKey = _cache.Keys.First();
                _cache.Remove(firstKey);
                #if UNITY_EDITOR
                Debug.Log($"[CachedPathfinding] LRU eviction: removed {firstKey}");
                #endif
            }

            _cache[cacheKey] = new List<Vector2Int>(path);

            #if UNITY_EDITOR
            Debug.Log($"[CachedPathfinding] Cache MISS: {start} → {goal}, cached result");
            #endif

            return path;
        }

        /// <summary>
        /// Pulisce la cache (utile tra scene o debug).
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            #if UNITY_EDITOR
            Debug.Log("[CachedPathfinding] Cache cleared");
            #endif
        }

        /// <summary>
        /// Ritorna stats della cache per monitoring.
        /// </summary>
        public int GetCacheSize() => _cache.Count;
        
        #endregion
    }
}
