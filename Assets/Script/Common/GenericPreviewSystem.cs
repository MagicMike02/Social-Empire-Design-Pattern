using UnityEngine;

namespace Script.Common
{
    /// <summary>
    /// Sistema generico per preview di GameObject durante placement/selezione.
    /// Ottimizzato per 2D isometrico (SpriteRenderer).
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
        private GameObject _currentPreview;
        private SpriteRenderer _previewSpriteRenderer;
        private Vector3 _lastPosition = Vector3.one * -9999f;
        private Vector3Int _lastGridCell = new Vector3Int(-9999, -9999, -9999);
        private bool _lastValidState = true;
        private string _previewName = "Preview";
        #endregion

        #region Properties
        public bool HasActivePreview => _currentPreview != null;
        public GameObject CurrentPreview => _currentPreview;
        #endregion

        #region Public Methods
        public void ShowPreview(GameObject prefab, Vector3 worldPosition, bool? isValid = null)
        {
            bool wasJustCreated = false;
            if (_currentPreview == null && prefab != null)
            {
                CreatePreview(prefab);
                wasJustCreated = true;
            }
            if (_currentPreview == null) return;
            
            SetPosition(worldPosition);
            
            if (wasJustCreated || (isValid.HasValue && _lastValidState != isValid.Value))
            {
                if (isValid.HasValue)
                    SetValidationState(isValid.Value);
                else
                    SetNeutralState();
            }
        }

        public bool UpdatePreviewIfMoved(Vector3 worldPosition, bool? isValid = null, float threshold = 0.01f)
        {
            if (_currentPreview == null) return false;
            float distance = Vector3.Distance(_lastPosition, worldPosition);
            if (distance < threshold && (!isValid.HasValue || _lastValidState == isValid.Value))
                return false;
            SetPosition(worldPosition);
            if (isValid.HasValue && _lastValidState != isValid.Value)
                SetValidationState(isValid.Value);
            return true;
        }

        /// <summary>
        /// Aggiorna la preview solo se la cella della griglia è cambiata.
        /// Wrapper per compatibilità con BuildingPlacer.
        /// Questo metodo è specifico per l'ottimizzazione grid-based (cella anziché distanza).
        /// </summary>
        public bool UpdatePreviewIfCellChanged(Vector3Int gridCell, Vector3 worldPosition, bool isValid)
        {
            // Se cella non è cambiata E validità è la stessa, non aggiornare
            if (gridCell == _lastGridCell && _lastValidState == isValid)
            {
                return false;
            }

            _lastGridCell = gridCell;
            SetPosition(worldPosition);

            if (_lastValidState != isValid)
            {
                SetValidationState(isValid);
            }

            return true;
        }

        public void SetScale(Vector3 scale)
        {
            if (_currentPreview != null)
                _currentPreview.transform.localScale = scale;
        }

        public void SetYOffset(float offset)
        {
            _yOffset = offset;
        }

        public void SetPreviewName(string name)
        {
            _previewName = name;
            if (_currentPreview != null)
                _currentPreview.name = name;
        }

        public void SetValidationColors(Color valid, Color invalid, Color? neutral = null)
        {
            _validColor = valid;
            _invalidColor = invalid;
            if (neutral.HasValue) _neutralColor = neutral.Value;
        }
        #endregion

        #region Private Methods
        private void CreatePreview(GameObject prefab)
        {
            _currentPreview = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            _currentPreview.name = _previewName;
            _previewSpriteRenderer = _currentPreview.GetComponent<SpriteRenderer>();
            if (_previewSpriteRenderer == null)
            {
                Debug.LogWarning($"[GenericPreviewSystem] Nessun SpriteRenderer trovato in '{prefab.name}'!");
                return;
            }
            _previewSpriteRenderer.sortingLayerName = _previewSortingLayer;
            _previewSpriteRenderer.sortingOrder = _previewSortingOrderOffset;
            
            // Disabilita collider per preview - non deve essere interagibile
            DisableCollider();
        }

        private void DisableCollider()
        {
            if (_currentPreview == null) return;
            Collider2D col2D = _currentPreview.GetComponent<Collider2D>();
            if (col2D != null)
                col2D.enabled = false;
        }
        private void SetPosition(Vector3 worldPosition)
        {
            if (_currentPreview == null) return;
            Vector3 targetPosition = worldPosition + Vector3.up * _yOffset;
            _currentPreview.transform.position = targetPosition;
            _lastPosition = worldPosition;
        }
        private void UpdatePreviewColor(Color targetColor)
        {
            if (_previewSpriteRenderer == null) return;
            _previewSpriteRenderer.color = targetColor;
        }
       
       
        public void HidePreview()
        {
            if (_currentPreview != null)
            {
                Destroy(_currentPreview);
                _currentPreview = null;
                _previewSpriteRenderer = null;
            }
            _lastPosition = Vector3.one * -9999f;
            _lastGridCell = new Vector3Int(-9999, -9999, -9999);
        }

        private void SetValidationState(bool isValid)
        {
            if (_lastValidState == isValid) return;
            _lastValidState = isValid;
            UpdatePreviewColor(isValid ? _validColor : _invalidColor);
        }

        private void SetNeutralState()
        {
            UpdatePreviewColor(_neutralColor);
        }

        
        #endregion

        #region Unity Lifecycle
        private void OnDestroy() { HidePreview(); }
        private void OnDisable() { HidePreview(); }
        #endregion
    }
}

