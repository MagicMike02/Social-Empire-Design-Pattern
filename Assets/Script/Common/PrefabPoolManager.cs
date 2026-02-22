using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace Script.Common
{
    /// <summary>
    /// Gestisce logiche di Object Pooling per GameObject prefabbricati (usato da Factory e PreviewSystem).
    /// </summary>
    public sealed class PrefabPoolManager : MonoBehaviour
    {
        [Tooltip("Transform parent in Hierarchy for deactivated pooled objects")]
        [SerializeField] private Transform _poolRoot;

        // Dizionario: Mappa ogni Prefab originale al suo ObjectPool specifico
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

        /// <summary>
        /// Prepara il container d'istanze padre per oggetti da ritirare dietro-quinte.
        /// </summary>
        private void Awake()
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("PoolRoot");
                go.transform.SetParent(this.transform);
                _poolRoot = go.transform;
            }
        }

        /// <summary>
        /// Ottiene un'istanza dal pool relativo al prefab specificato.
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();

            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;

            return instance;
        }

        /// <summary>
        /// Rilascia un'istanza nel pool del suo prefab originale.
        /// ATTENZIONE: Il chiamante deve assicurarsi di passare il prefab ORIGINALE come chiave.
        /// </summary>
        public void Release(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null) return;

            if (_pools.TryGetValue(prefab, out var pool))
            {
                pool.Release(instance);
            }
            else
            {
                Debug.LogWarning($"[PrefabPoolManager] Tentativo di rilascio di un'istanza ({instance.name}) per la quale non esiste un pool registrato ({prefab.name}). Distruzione...");
                Destroy(instance);
            }
        }

        /// <summary>
        /// Crea una memory pool object a runtime o ne estrae una archiviata se la chiamata e' ricorrente.
        /// </summary>
        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool))
            {
                return pool;
            }

            // Factory method per quando la pool e' vuota
            GameObject CreateFunc()
            {
                var instance = Instantiate(prefab);
                instance.name = $"{prefab.name}_Pooled";
                return instance;
            }

            // Callback quando viene preso dal pool
            void ActionOnGet(GameObject obj)
            {
                obj.SetActive(true);
            }

            // Callback quando viene rilasciato nel pool
            void ActionOnRelease(GameObject obj)
            {
                obj.SetActive(false);
                obj.transform.SetParent(_poolRoot);
            }

            // Callback quando il pool eccede la maxCapacity
            void ActionOnDestroy(GameObject obj)
            {
                Destroy(obj);
            }

            var newPool = new ObjectPool<GameObject>(
                createFunc: CreateFunc,
                actionOnGet: ActionOnGet,
                actionOnRelease: ActionOnRelease,
                actionOnDestroy: ActionOnDestroy,
                collectionCheck: true, // Utility debug
                defaultCapacity: 10,
                maxSize: 100
            );

            _pools.Add(prefab, newPool);
            return newPool;
        }
    }
}
