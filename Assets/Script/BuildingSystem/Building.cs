using Script.Core.Events;
using UnityEngine;

namespace Script.BuildingSystem
{
    public sealed class Building : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        public BuildingConfigSO Config { get; private set; }

        public void Init(BuildingConfigSO config)
        {
            if (config == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[Building] Init chiamato con config null!");
#endif
                return;
            }

            Config = config;

            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_renderer != null)
            {
                _renderer.sortingLayerName = config.SortingLayer;
                _renderer.sortingOrder = config.BaseSortingOrder;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[Building] SpriteRenderer mancante sul prefab: {name}");
#endif
            }
        }

        /// <summary>
        /// Aggiorna il sorting order in base alla posizione Y per la visualizzazione isometrica.
        /// Delega a <see cref="IsometricSortingUtility.ApplySorting"/> per evitare duplicazione logica.
        /// </summary>
        /// <param name="yBase">Posizione Y base per il calcolo</param>
        public void SetSortingByY(float yBase)
        {
            if (_renderer == null || Config == null) return;

            IsometricSortingUtility.ApplySorting(
                _renderer,
                Config.SortingLayer,
                yBase,
                Config.BaseSortingOrder);
        }

        private void OnDestroy()
        {
            // Notifica distruzione edificio
            GlobalEventBus.Publish(new BuildingDestroyedEvent());
        }
    }
}
