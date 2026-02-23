using Script.InputSystem;
using UnityEngine;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Rappresenta un'istanza fisica di una risorsa nel mondo di gioco.
    /// Gestisce l'interazione del giocatore (IHoverable) e la sua visualizzazione.
    /// </summary>
    public class ResourceInstance : MonoBehaviour, IHoverable
    {
        #region Private Fields
        
        private ResourceDataSO _data;
        private Vector2Int _gridPosition;
        private ResourceManager _manager;
        private SpriteRenderer _spriteRenderer;
        private int _prefabIndex;
        private bool _isCollected;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Dati statici associati a questa istanza di risorsa.
        /// </summary>
        public ResourceDataSO Data => _data;
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        #endregion

        #region Initialization

        /// <summary>
        /// Inizializza l'istanza con i suoi dati e la collega al ResourceManager.
        /// </summary>
        public void Initialize(ResourceDataSO data, Vector2Int gridPos, ResourceManager manager)
        {
            _data = data;
            _gridPosition = gridPos;
            _manager = manager;
            _isCollected = false;
            tag = "Resources";
            SetSortingOrder();
        }

        #endregion
        
        #region Internal Helpers

        private void SetSortingOrder()
        {
            if (_spriteRenderer)
            {
                // Più in basso sull’asse Y -> sortingOrder maggiore -> disegnato sopra
                _spriteRenderer.sortingOrder = -(int)(transform.position.y * 100);
            }
            else
            {
                // Aggiunto un warning per aiutare a debuggare se il SpriteRenderer non viene trovato.
                Debug.LogWarning($"SpriteRenderer non trovato su {gameObject.name} o sui suoi figli per l'ordinamento.");
            }
        }


        #endregion

        #region Input Handlers (IHoverable)

        public void OnHoverEnter()
        {
            // Optional: future visual feedback (glow, outline, etc.)
        }

        public void OnHoverExit()
        {
            // Optional: remove visual feedback
        }

        public void OnClick()
        {
            CollectResource();
        }

        public void OnRightClick(Vector3 worldPosition)
        {
            // Future: right-click commands if needed
        }
        
        #endregion
        
        #region Resource Logic

        private void CollectResource()
        {
            if (_isCollected) return;

            if (_manager == null || _data == null)
            {
                Debug.LogWarning($"[ResourceInstance] Impossibile raccogliere risorsa: Manager o Data non inizializzati su {gameObject.name}");
                return;
            }
            
            _isCollected = true;
            _manager.HandleResourceCollected(_gridPosition, _data);
        }
        
        #endregion
    }
}