using UnityEngine;
using VContainer;

namespace Script.Common
{
    /// <summary>
    /// Sistema generico per preview di GameObject durante placement/selezione.
    /// Ottimizzato per 2D isometrico (SpriteRenderer) con Object Pooling.
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
        private GameObject _currentPrefabRef; // Memorizza il reference al prefab per capire quando l'utente cambia edificio
        private SpriteRenderer _previewSpriteRenderer;
        private Vector3 _lastPosition = Vector3.one * -9999f;
        private Vector3Int _lastGridCell = new Vector3Int(-9999, -9999, -9999);
        private bool _lastValidState = true;
        private string _previewName = "Preview";
        
        // Injected dependency
        private PrefabPoolManager _poolManager;
        #endregion

        [Inject]
        public void Construct(PrefabPoolManager poolManager)
        {
            try
            {
                _poolManager = poolManager;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"[GenericPreviewSystem] Errore durante Construct: {ex.Message}");
#endif
            }
        }

        #region Properties
        public bool HasActivePreview => _currentPreview != null;
        public GameObject CurrentPreview => _currentPreview;
        #endregion

        #region Public Methods

        /// <summary>
        /// Instanzia o recupera dal pool il prefab da usare come preview visuale, aggiornandone lo stato.
        /// </summary>
        public void ShowPreview(GameObject prefab, Vector3 worldPosition, bool? isValid = null)
        {
            if (prefab == null) return;
           
            bool prefabChanged = _currentPrefabRef != null && _currentPrefabRef != prefab;
            if (prefabChanged && _currentPreview != null)
            {
                ReleaseToPool();
            }

            bool wasJustCreated = false;
            // Se non avevamo già una preview visibile, la creiamo dal Pool
            if (_currentPreview == null)
            {
                CreatePreview(prefab);
                wasJustCreated = true;
            }
            
            SetPosition(worldPosition);
            
            if (wasJustCreated || (isValid.HasValue && _lastValidState != isValid.Value))
            {
                if (isValid.HasValue)
                    SetValidationState(isValid.Value);
                else
                    SetNeutralState();
            }
        }

        /// <summary>
        /// Aggiorna la posizione della preview corrente se si sposta di un _threshold_ fisso, o se cambia validità.
        /// </summary>
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

        /// <summary>
        /// Imposta la scala uniforme della preview visuale in gioco.
        /// </summary>
        public void SetScale(Vector3 scale)
        {
            if (_currentPreview != null)
                _currentPreview.transform.localScale = scale;
        }

        /// <summary>
        /// Imposta un offset sull'asse Y utile per arginare conflitti di z-fighting.
        /// </summary>
        public void SetYOffset(float offset)
        {
            _yOffset = offset;
        }

        /// <summary>
        /// Rinomina il GameObject della preview utilizzandolo anche per le label a fine di debug.
        /// </summary>
        public void SetPreviewName(string name)
        {
            _previewName = name;
            if (_currentPreview != null)
                _currentPreview.name = name;
        }

        /// <summary>
        /// Sovrascrive la colorazione moltiplicativa usata per comunicare lo stato della preview all'utente.
        /// </summary>
        public void SetValidationColors(Color valid, Color invalid, Color? neutral = null)
        {
            _validColor = valid;
            _invalidColor = invalid;
            if (neutral.HasValue) _neutralColor = neutral.Value;
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Estrae o genera l'oggetto base della preview preparandone i componenti visuali (SpriteRenderer, ecc.).
        /// </summary>
        private void CreatePreview(GameObject prefab)
        {
            if (_poolManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[GenericPreviewSystem] PoolManager non inizializzato!");
#endif
                return;
            }

            _currentPrefabRef = prefab; // Salviamo il master object
            _currentPreview = _poolManager.Get(prefab, Vector3.zero, Quaternion.identity);
            
            _currentPreview.name = _previewName;
            _previewSpriteRenderer = _currentPreview.GetComponentInChildren<SpriteRenderer>();
            
            if (_previewSpriteRenderer == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[GenericPreviewSystem] Nessun SpriteRenderer trovato in '{prefab.name}'!");
#endif
                return;
            }
            _previewSpriteRenderer.sortingLayerName = _previewSortingLayer;
            _previewSpriteRenderer.sortingOrder = _previewSortingOrderOffset;
            
            // Disabilita collider per preview - non deve essere interagibile
            DisableCollider();
        }

        /// <summary>
        /// Silenzia interazioni input sulla preview durante il placement.
        /// </summary>
        private void DisableCollider()
        {
            if (_currentPreview == null) return;
            Collider2D col2D = _currentPreview.GetComponent<Collider2D>();
            if (col2D != null)
                col2D.enabled = false;
        }
        /// <summary>
        /// Processa il movimento applicando all'oggetto l'offset dedicato.
        /// </summary>
        private void SetPosition(Vector3 worldPosition)
        {
            if (_currentPreview == null) return;
            Vector3 targetPosition = worldPosition + Vector3.up * _yOffset;
            _currentPreview.transform.position = targetPosition;
            _lastPosition = worldPosition;
        }
        /// <summary>
        /// Tinteggia lo shader nativo della SpriteRenderer corrente senza alterare la base texture.
        /// </summary>
        private void UpdatePreviewColor(Color targetColor)
        {
            if (_previewSpriteRenderer == null) return;
            _previewSpriteRenderer.color = targetColor;
        }
       
       
        /// <summary>
        /// Nasconde ed ecclissa formalmente la preview dal gioco, smontandone posizionamento ed eventi temporanei.
        /// </summary>
        public void HidePreview()
        {
            if (_currentPreview != null)
            {
                ReleaseToPool();
            }
            _lastPosition = Vector3.one * -9999f;
            _lastGridCell = new Vector3Int(-9999, -9999, -9999);
        }

        /// <summary>
        /// Termina il ciclo della preview confermando l'oggetto per objectpooling pulendone preventivamente la tint color.
        /// </summary>
        private void ReleaseToPool()
        {
            if (_currentPreview == null) return;

            // Ripristina il neutral state sul renderer prima di parcheggiarlo
            if (_previewSpriteRenderer != null)
                _previewSpriteRenderer.color = Color.white;
            
            // Usa il pool manager se disponibile, altrimenti distruggi come fallback
            if (_poolManager != null && _currentPrefabRef != null)
            {
                _poolManager.Release(_currentPrefabRef, _currentPreview);
            }
            else
            {
                Destroy(_currentPreview);
            }
            
            _currentPreview = null;
            _currentPrefabRef = null;
            _previewSpriteRenderer = null;
        }

        /// <summary>
        /// Cambia il feedback tint visivo in accordo col flag di validation indicato.
        /// </summary>
        private void SetValidationState(bool isValid)
        {
            if (_lastValidState == isValid) return;
            _lastValidState = isValid;
            UpdatePreviewColor(isValid ? _validColor : _invalidColor);
        }

        /// <summary>
        /// Ripristina la colorazione neutra per visualizzazioni che ignorano concept di placement e invalidazione spazio.
        /// </summary>
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
