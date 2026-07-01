using System.Collections.Generic;
using UnityEngine;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Sistema dedicato all'object pooling nativo delle instanze risorsa.
    /// Utilizzato da ResourceSpawner e ResourceManager.
    /// </summary>
    public class ResourcePoolManager : MonoBehaviour
    {
        #region Inner Classes
        
        [System.Serializable]
        public class Pool
        {
            public ResourceDataSO resourceData;
            public int initialSize = 10;
            public Transform parent;
        }
        
        #endregion

        #region Private Fields
        
        [SerializeField] private List<Pool> pools;
        private Dictionary<ResourceDataSO, Pool> _poolDict = new();
        private Dictionary<ResourceDataSO, Queue<GameObject>> _queues = new();
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            foreach (var pool in pools)
            {
                if (!pool.resourceData)
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning("[PoolManager] ResourceDataSO nullo in pool!");
                    #endif
                    continue;
                }

                _poolDict.TryAdd(pool.resourceData, pool);

                var queue = new Queue<GameObject>();
                _queues[pool.resourceData] = queue;

                for (var i = 0; i < pool.initialSize; i++)
                {
                    var prefab = pool.resourceData.GetRandomPrefab();
                    if (!prefab) continue;

                    var go = Instantiate(prefab, pool.parent);
                    go.SetActive(false);
                    queue.Enqueue(go);
                }
            }
        }

        #endregion

        #region Public APIs

        /// <summary>
        /// Esegue un Dequeue dal pool di GameObject della specifica risorsa, instanziandolo se vuoto.
        /// </summary>
        public GameObject GetFromPool(ResourceDataSO data, Vector3 position, Quaternion rotation)
        {
            if (!_poolDict.TryGetValue(data, out var pool) || !_queues.TryGetValue(data, out var queue)) return null;

            GameObject go;
            if (queue.Count > 0)
            {
                go = queue.Dequeue();
            }
            else
            {
                var prefab = data.GetRandomPrefab();
                if (!prefab) return null;

                go = Instantiate(prefab, pool.parent);
            }

            go.transform.position = position;
            go.transform.rotation = rotation;

            go.SetActive(true);
            return go;
        }

        /// <summary>
        /// Resituisce l'oggetto risorsa al pool designato nascondendolo dalla scena.
        /// </summary>
        public void ReturnToPool(GameObject go, ResourceDataSO data)
        {
            if (!_poolDict.TryGetValue(data, out _) || !_queues.TryGetValue(data, out var queue))
            {
                Destroy(go);
                return;
            }

            go.SetActive(false);
            queue.Enqueue(go);
        }
        
        #endregion
    }
}
