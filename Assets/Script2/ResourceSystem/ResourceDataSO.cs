using System.Collections.Generic;
using Script2.ResourceSystem.Enums;
using UnityEngine;

namespace Script2.ResourceSystem
{
    [CreateAssetMenu(fileName = "NewResourceData", menuName = "ScriptableObjects/ResourceDataSO")]
    public class ResourceDataSO : ScriptableObject
    {
        [Header("General Info")] public string resourceName;
        public List<GameObject> prefabs;
        public GameObject _regenPrefab;

        [Header("Resource info")] public ResourceType resourceType;
        public int collectedAmount = 10; // Quantità di risorsa che si ottiene quando viene raccolta

        [Header("Generation Settings")] [Tooltip("Numero di gruppi che possono essere generati sulla mappa")]
        public int groupCount = 3;

        public int defaultGroupSize = 15;

        [Tooltip("Dimensioni possibili dei gruppi (es: 3, 5, 7)")]
        public List<int> possibleGroupSizes = new List<int> { 3, 5, 7, 9 };

        [Header("Interaction Settings")]
        [Tooltip("Il tempo in secondi dopo cui la risorsa rigenera (se non è distrutta)")]
        public float regenerationTime = 3600f;

        [Tooltip("Se true, la risorsa scompare dopo essere raccolta")]
        public bool isDestroyedOnCollect = true;

        public float yOffset = 0;

        /// <summary>
        /// Restituisce un prefab casuale dalla lista
        /// </summary>
        public GameObject GetRandomPrefab()
        {
            if (prefabs == null || prefabs.Count == 0) return null;
            return prefabs[Random.Range(0, prefabs.Count)];
        }
    }
}