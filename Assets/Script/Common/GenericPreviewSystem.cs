using UnityEngine;
using VContainer;

namespace Script.Common
{
    /// <summary>
    /// Sistema generico per preview di GameObject durante placement/selezione.
    /// Orchestratore: delega la gestione del GameObject a <see cref="PreviewPool"/>
    /// e la colorazione a <see cref="PreviewTintController"/>.
    /// Mantiene il tracking della posizione/cella e l'offset Y.
    /// </summary>
    public class GenericPreviewSystem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Preview Settings")]
        [Tooltip("Offset Y per evitare z-fighting")]
        [SerializeField] private float _yOffset = 0.05f;
        [Tooltip("Sorting layer per preview")]
        [SerializeField] private string _previewSortingLayer = "OnTiles";
        [Tooltip("Sorting order offset per preview")]
        [SerializeField] private int _previewSortingOrderOffset = 100;
        [Tooltip("Tint verde per stato Valido (moltiplicativo, mantiene texture)")]
        [SerializeField] private Color _validColor = new Color(0.7f, 1f, 0.7f, 0.8f);
        [Tooltip("Tint rosso per stato Invalido (moltiplicativo, mantiene texture)")]
        [SerializeField] private Color _invalidColor = new Color(1f, 0.7f, 0.7f, 0.8f);
        [Tooltip("Tint neutro (nessuna validazione, mantiene texture)")]
        [SerializeField] private Color _neutralColor = new Color(1f, 1f, 1f, 0.8f);

        #endregion

        #region Private Fields

        private Vector3 _lastPosition = Vector3.one * -9999f;
        private Vector3Int _lastGridCell = new Vector3Int(-9999, -9999, -9999);

        // Injected dependency
        private PrefabPoolManager _poolManager;

        // Delegati
        private PreviewPool _pool;
        private PreviewTintController _tint;

        #endregion

        [Inject]
        public void Construct(PrefabPoolManager poolManager, PreviewPool previewPool, PreviewTintController tintController)
        {
            try
            {
                _poolManager = poolManager;
                _pool = previewPool;
                _tint = tintController;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[GenericPreviewSystem] Errore durante Construct: {ex.Message}");
#endif
            }
        }

        #region Properties

        public bool HasActivePreview => _pool?.HasActivePreview ?? false;
        public GameObject CurrentPreview => _pool?.CurrentPreview;

        #endregion

        #region Public API

        /// <summary>
        /// Instanzia o recupera dal pool il prefab da usare come preview visuale, aggiornandone lo stato.
        /// </summary>
        public void ShowPreview(GameObject prefab, Vector3 worldPosition, bool? isValid = null)
        {
            if (prefab == null || _pool == null) return;

            bool prefabChanged = _pool.HasActivePreview && _pool.CurrentPreview != null
                && prefab != _pool.CurrentPreview;

            if (prefabChanged)
                _pool.Release();

            bool wasJustCreated = !_pool.HasActivePreview;
            if (wasJustCreated)
            {
                _pool.CreatePreview(prefab, _previewSortingLayer, _previewSortingOrderOffset);
                _tint.SetValidationColors(_validColor, _invalidColor, _neutralColor);
            }

            SetPosition(worldPosition);

            if (wasJustCreated || (isValid.HasValue && _tint.LastValidState != isValid.Value))
            {
                if (isValid.HasValue)
                    _tint.ForceValidationState(_pool.CurrentSpriteRenderer, isValid.Value);
                else
                    _tint.ApplyNeutralState(_pool.CurrentSpriteRenderer);
            }
        }

        /// <summary>
        /// Aggiorna la posizione della preview corrente se si sposta di un threshold fisso, o se cambia validità.
        /// </summary>
        public bool UpdatePreviewIfMoved(Vector3 worldPosition, bool? isValid = null, float threshold = 0.01f)
        {
            if (!_pool?.HasActivePreview ?? true) return false;

            float distance = Vector3.Distance(_lastPosition, worldPosition);
            if (distance < threshold && (!isValid.HasValue || _tint.LastValidState == isValid.Value))
                return false;

            SetPosition(worldPosition);

            if (isValid.HasValue && _tint.LastValidState != isValid.Value)
                _tint.ForceValidationState(_pool.CurrentSpriteRenderer, isValid.Value);

            return true;
        }

        /// <summary>
        /// Aggiorna la preview solo se la cella della griglia è cambiata.
        /// Wrapper per compatibilità con BuildingPlacer (ottimizzazione grid-based).
        /// </summary>
        public bool UpdatePreviewIfCellChanged(Vector3Int gridCell, Vector3 worldPosition, bool isValid)
        {
            if (!_pool?.HasActivePreview ?? true) return false;

            if (gridCell == _lastGridCell && _tint.LastValidState == isValid)
                return false;

            _lastGridCell = gridCell;
            SetPosition(worldPosition);

            if (_tint.LastValidState != isValid)
                _tint.ForceValidationState(_pool.CurrentSpriteRenderer, isValid);

            return true;
        }

        /// <summary>
        /// Imposta la scala uniforme della preview visuale.
        /// </summary>
        public void SetScale(Vector3 scale) => _pool?.SetScale(scale);

        /// <summary>
        /// Imposta un offset sull'asse Y per arginare conflitti di z-fighting.
        /// </summary>
        public void SetYOffset(float offset) => _yOffset = offset;

        /// <summary>
        /// Rinomina il GameObject della preview.
        /// </summary>
        public void SetPreviewName(string name) => _pool?.SetPreviewName(name);

        /// <summary>
        /// Sovrascrive la colorazione moltiplicativa per gli stati valid/invalid/neutral.
        /// </summary>
        public void SetValidationColors(Color valid, Color invalid, Color? neutral = null)
        {
            _validColor = valid;
            _invalidColor = invalid;
            if (neutral.HasValue) _neutralColor = neutral.Value;
            _tint?.SetValidationColors(valid, invalid, neutral);
        }

        /// <summary>
        /// Nasconde e rilascia la preview al pool, resettando il tracking posizione/cella.
        /// </summary>
        public void HidePreview()
        {
            _pool?.Release();
            _lastPosition = Vector3.one * -9999f;
            _lastGridCell = new Vector3Int(-9999, -9999, -9999);
        }

        #endregion

        #region Private Methods

        private void SetPosition(Vector3 worldPosition)
        {
            if (_pool?.HasActivePreview != true) return;
            Vector3 targetPosition = worldPosition + Vector3.up * _yOffset;
            _pool.SetPosition(targetPosition);
            _lastPosition = worldPosition;
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy() => HidePreview();
        private void OnDisable() => HidePreview();

        #endregion
    }
}
