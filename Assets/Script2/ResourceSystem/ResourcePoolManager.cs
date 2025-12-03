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
            public Queue<GameObject> objects = new();
        }

        [SerializeField] private List<Pool> pools;
        private Dictionary<ResourceDataSO, Pool> _poolDict = new();

        private void Awake()
        {
            foreach (var pool in pools)
            {
                if (!pool.resourceData)
                {
                    Debug.LogWarning("[PoolManager] ResourceDataSO nullo in pool!");
                    continue;
                }

                _poolDict.TryAdd(pool.resourceData, pool);

                for (var i = 0; i < pool.initialSize; i++)
                {
                    var prefab = pool.resourceData.GetRandomPrefab();
                    if (!prefab) continue;

                    var go = Instantiate(prefab, pool.parent);
                    go.SetActive(false);
                    pool.objects.Enqueue(go);
                }
            }
        }

        public GameObject GetFromPool(ResourceDataSO data, Vector3 position, Quaternion rotation)
        {
            if (!_poolDict.TryGetValue(data, out var pool)) return null;

            GameObject go;
            if (pool.objects.Count > 0)
            {
                go = pool.objects.Dequeue();
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

        public void ReturnToPool(GameObject go, ResourceDataSO data)
        {
            if (!_poolDict.TryGetValue(data, out var pool))
            {
                Destroy(go);
                return;
            }

            go.SetActive(false);
            pool.objects.Enqueue(go);
        }
    }
}