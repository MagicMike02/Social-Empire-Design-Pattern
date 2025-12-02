using System.Collections.Generic;
using UnityEngine;

namespace Script2.ResourceSystem
{
    public class ResourcePoolManager : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            public ResourceDataSO resourceData;
            public int initialSize = 10;
            public Transform parent;
            [HideInInspector] public Queue<GameObject> objects = new();
        }

        [SerializeField] private List<Pool> pools;
        private Dictionary<ResourceDataSO, Pool> _poolDict = new();

        private void Awake()
        {
            foreach (var pool in pools)
            {
                if (pool.resourceData == null) {
                    Debug.LogWarning("[PoolManager] ResourceDataSO nullo in pool!");
                    continue;
                }
                if (!_poolDict.ContainsKey(pool.resourceData))
                    _poolDict.Add(pool.resourceData, pool);
                for (int i = 0; i < pool.initialSize; i++)
                {
                    var prefab = pool.resourceData.GetRandomPrefab();
                    if (prefab == null) {
                        Debug.LogWarning($"[PoolManager] Prefab nullo per {pool.resourceData.name}");
                        continue;
                    }
                    var go = Instantiate(prefab, pool.parent);
                    Debug.Log($"[PoolManager] Istanzio {prefab.name} per {pool.resourceData.name}");
                    go.SetActive(false);
                    pool.objects.Enqueue(go);
                }
            }
        }

        public GameObject GetFromPool(ResourceDataSO data, Vector3 position, Quaternion rotation)
        {
            if (!_poolDict.TryGetValue(data, out var pool)) {
                Debug.LogWarning($"[PoolManager] Nessun pool trovato per {data.name}");
                return null;
            }
            GameObject go;
            if (pool.objects.Count > 0)
            {
                go = pool.objects.Dequeue();
                Debug.Log($"[PoolManager] Recupero {go.name} dal pool di {data.name}");
            }
            else
            {
                var prefab = data.GetRandomPrefab();
                if (prefab == null) {
                    Debug.LogWarning($"[PoolManager] Prefab nullo per {data.name} (fallback)");
                    return null;
                }
                go = Instantiate(prefab, pool.parent);
                Debug.Log($"[PoolManager] Istanzio nuovo {prefab.name} per {data.name} (pool esaurito)");
            }
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.SetActive(true);
            return go;
        }

        public void ReturnToPool(GameObject go, ResourceDataSO data)
        {
            if (!_poolDict.TryGetValue(data, out var pool)) {
                Debug.LogWarning($"[PoolManager] Nessun pool per {data.name}, distruggo oggetto.");
                Destroy(go);
                return;
            }
            go.SetActive(false);
            pool.objects.Enqueue(go);
            Debug.Log($"[PoolManager] Restituisco {go.name} al pool di {data.name}");
        }
    }
}
