using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Sistema preview per edifici 2D isometrici durante placement.
    /// Ottimizzato per SpriteRenderer 2D - Nessun supporto 3D necessario.
    /// Features: Colori puri, trasparenza, cache anti-glitch.
    /// </summary>
    public class PreviewSystem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Preview Settings")] [Tooltip("Offset Y per evitare z-fighting con tile")] [SerializeField]
        private float _previewYOffset = 0.05f;

        [Tooltip("Sorting layer per preview edificio")] [SerializeField]
        private string _previewSortingLayer = "OnTiles";

        [Tooltip("Sorting order offset per preview (relativo alla posizione)")] [SerializeField]
        private int _previewSortingOrderOffset = 100;

        [Tooltip("Tint verde per posizione valida (moltiplicativo, mantiene texture)")] [SerializeField]
        private Color _validColor = new Color(0.7f, 1f, 0.7f, 0.8f);

        [Tooltip("Tint rosso per posizione invalida (moltiplicativo, mantiene texture)")] [SerializeField]
        private Color _invalidColor = new Color(1f, 0.7f, 0.7f, 0.8f);

        #endregion

        #region Private Fields

        private GameObject _currentPreviewInstance;
        private SpriteRenderer _previewSpriteRenderer;
        private Vector3Int _lastPreviewCell = new Vector3Int(-9999, -9999, -9999);
        private bool _lastValidState = true;

        #endregion

        #region Public Methods

        /// <summary>
        /// Crea o aggiorna la preview dell'edificio.
        /// </summary>
        /// <param name="prefab">Prefab edificio da mostrare</param>
        /// <param name="worldPosition">Posizione world (con offset automatico)</param>
        /// <param name="isValid">True se posizione valida, False altrimenti</param>
        public void ShowPreview(GameObject prefab, Vector3 worldPosition, bool isValid)
        {
            // Crea preview se non esiste
            bool wasJustCreated = false;
            if (_currentPreviewInstance == null && prefab != null)
            {
                CreatePreview(prefab);
                wasJustCreated = true;
            }

            if (_currentPreviewInstance == null) return;

            // Aggiorna posizione con offset anti z-fighting
            Vector3 targetPosition = worldPosition + Vector3.up * _previewYOffset;
            _currentPreviewInstance.transform.position = targetPosition;

            if (wasJustCreated || _lastValidState != isValid)
            {
                UpdatePreviewColor(isValid);
                _lastValidState = isValid;
            }
        }

        /// <summary>
        /// Aggiorna la posizione della preview SOLO se la cella griglia è cambiata.
        /// Previene glitch da update continui.
        /// </summary>
        /// <param name="gridCell">Cella griglia corrente</param>
        /// <param name="worldPosition">Posizione world corrispondente</param>
        /// <param name="isValid">Validità posizione</param>
        /// <returns>True se la preview è stata aggiornata</returns>
        public bool UpdatePreviewIfCellChanged(Vector3Int gridCell, Vector3 worldPosition, bool isValid)
        {
            // OTTIMIZZAZIONE: Aggiorna solo se cella cambiata
            if (gridCell == _lastPreviewCell && _lastValidState == isValid)
            {
                return false;
            }

            _lastPreviewCell = gridCell;

            if (_currentPreviewInstance != null)
            {
                Vector3 targetPosition = worldPosition + Vector3.up * _previewYOffset;
                _currentPreviewInstance.transform.position = targetPosition;
            }

            // Aggiorna colore se stato cambiato
            if (_lastValidState != isValid)
            {
                UpdatePreviewColor(isValid);
                _lastValidState = isValid;
            }

            return true;
        }

        /// <summary>
        /// Nasconde e distrugge la preview corrente.
        /// </summary>
        public void HidePreview()
        {
            if (_currentPreviewInstance != null)
            {
                Destroy(_currentPreviewInstance);
                _currentPreviewInstance = null;
                _previewSpriteRenderer = null;
            }

            _lastPreviewCell = new Vector3Int(-9999, -9999, -9999);
        }

        #endregion

        #region Private Methods

        private void CreatePreview(GameObject prefab)
        {
            _currentPreviewInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            _currentPreviewInstance.name = "BuildingPreview";

            _previewSpriteRenderer = _currentPreviewInstance.GetComponent<SpriteRenderer>();

            if (_previewSpriteRenderer == null)
            {
                Debug.LogWarning("[PreviewSystem] Nessun SpriteRenderer trovato nel prefab preview! Assicurati che il prefab edificio abbia un SpriteRenderer.");
                return;
            }

            // Imposta sorting layer e order per preview
            _previewSpriteRenderer.sortingLayerName = _previewSortingLayer;
            _previewSpriteRenderer.sortingOrder = _previewSortingOrderOffset;

            // Disabilita collider per preview
            DisableCollider();
        }

        private void DisableCollider()
        {
            Collider2D col2D = _currentPreviewInstance.GetComponent<Collider2D>();
            col2D.enabled = false;
        }

        private void UpdatePreviewColor(bool isValid)
        {
            if (_previewSpriteRenderer == null) return;

            Color targetColor = isValid ? _validColor : _invalidColor;

            _previewSpriteRenderer.color = targetColor;
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            HidePreview();
        }

        private void OnDisable()
        {
            HidePreview();
        }

        #endregion
    }
}