using UnityEngine;

namespace Script.Core.Entities
{
    /// <summary>
    /// Reusable visual highlight component for any entity prefab.
    /// Uses separate child GameObjects for each highlight state:
    /// - <c>SelectionCircle</c>: shown when Selected (circle sprite)
    /// - <c>HoverOutline</c>: shown when Hovered (white outline sprite)
    /// Falls back to tinting the entity's own <see cref="SpriteRenderer"/> if no child is found.
    /// </summary>
    public sealed class HighlightableComponent : MonoBehaviour, IHighlightable
    {
        #region Fields

        private SpriteRenderer _mainRenderer;
        private Color _mainBaseColor;
        private bool _isInitialized;

        // Child GameObjects per stato (cercati per nome in Awake)
        private GameObject _selectionCircle;
        private SpriteRenderer _selectionRenderer;
        private GameObject _hoverOutline;
        private SpriteRenderer _hoverRenderer;

        [Header("Child Names (cercati in Awake)")]
        [SerializeField] private string selectionCircleName = "SelectionCircle";
        [SerializeField] private string hoverOutlineName = "HoverOutline";

        [Header("Tint Colors (fallback se child assenti)")]
        [SerializeField] private Color hoverColor = new(1f, 1f, 0.8f, 1f);
        [SerializeField] private Color selectedColor = new(0.6f, 1f, 0.6f, 1f);
        [SerializeField] private Color previewColor = new(0.6f, 0.8f, 1f, 0.7f);
        [SerializeField] private Color invalidColor = new(1f, 0.4f, 0.4f, 0.7f);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mainRenderer = GetComponent<SpriteRenderer>();
            if (_mainRenderer != null)
                _mainBaseColor = _mainRenderer.color;

            // Cerca i child GameObject per nome (configurabile da Inspector)
            _selectionCircle = transform.Find(selectionCircleName)?.gameObject;
            if (_selectionCircle != null)
            {
                _selectionRenderer = _selectionCircle.GetComponent<SpriteRenderer>();
                _selectionCircle.SetActive(false);
            }

            _hoverOutline = transform.Find(hoverOutlineName)?.gameObject;
            if (_hoverOutline != null)
            {
                _hoverRenderer = _hoverOutline.GetComponent<SpriteRenderer>();
                _hoverOutline.SetActive(false);
            }

            _isInitialized = true;
        }

        #endregion

        #region IHighlightable

        /// <inheritdoc/>
        public void Highlight(HighlightType type)
        {
            if (!_isInitialized) return;

            // Disattiva tutti i child prima di attivare quello corretto
            DisableAllChildren();

            switch (type)
            {
                case HighlightType.Hover:
                    if (_hoverOutline != null)
                    {
                        _hoverOutline.SetActive(true);
                        ApplyChildSorting(_hoverRenderer, offset: 5);
                        return;
                    }
                    // Fallback: tint del renderer principale
                    if (_mainRenderer != null) _mainRenderer.color = hoverColor;
                    break;

                case HighlightType.Selected:
                    if (_selectionCircle != null)
                    {
                        _selectionCircle.SetActive(true);
                        ApplyChildSorting(_selectionRenderer, offset: -20);
                        return;
                    }
                    // Fallback: tint del renderer principale
                    if (_mainRenderer != null) _mainRenderer.color = selectedColor;
                    break;

                case HighlightType.Preview:
                case HighlightType.Invalid:
                    // Preview/Invalid usano il tint del renderer principale
                    // (coerente con GenericPreviewSystem che gestisce la preview separatamente)
                    if (_mainRenderer != null)
                        _mainRenderer.color = type == HighlightType.Preview ? previewColor : invalidColor;
                    break;

                default:
                    ClearHighlight();
                    break;
            }
        }

        /// <inheritdoc/>
        public void ClearHighlight()
        {
            if (!_isInitialized) return;

            DisableAllChildren();

            // Ripristina colore originale del renderer principale
            if (_mainRenderer != null)
                _mainRenderer.color = _mainBaseColor;
        }

        #endregion

        #region Private Methods

        private void DisableAllChildren()
        {
            if (_selectionCircle != null) _selectionCircle.SetActive(false);
            if (_hoverOutline != null) _hoverOutline.SetActive(false);
        }

        /// <summary>
        /// Sincronizza sorting layer/order del child highlight con il renderer principale,
        /// applicando un offset per posizionarlo sopra (outline) o sotto (circle).
        /// </summary>
        private void ApplyChildSorting(SpriteRenderer childRenderer, int offset)
        {
            if (childRenderer == null || _mainRenderer == null) return;

            childRenderer.sortingLayerName = _mainRenderer.sortingLayerName;
            childRenderer.sortingOrder = _mainRenderer.sortingOrder + offset;
        }

        #endregion
    }
}
