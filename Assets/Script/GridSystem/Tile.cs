using Script.BuildingSystem;
using Script.InputSystem;
using TMPro;
using UnityEngine;

namespace Script.GridSystem
{
    /// <summary>
    /// Represents a single tile in the isometric grid.
    /// Implements IGridEntity to provide authoritative grid position (no mathematical conversion needed).
    /// Implements IHoverable for input system integration.
    /// </summary>
    public class Tile : MonoBehaviour, IHoverable, IGridEntity
    {
        #region Constants & Inspector
        
        private const string DefaultShaderName = "Sprites/Default";
        
        [SerializeField] private TextMeshPro _coordinatesText;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor = Color.yellow;
        
        [SerializeField] private Color _lockedColor = Color.grey; //verde grigio
        [SerializeField] private Color _buyableColor = Color.grey; //verde verde;
        [SerializeField] private Color _unlockedColor = Color.white;//verde normale;
        
        #endregion
        
        #region State & Fields
        
        public TileState State { private set; get; }
        
        // Source of Truth: grid position (cached at Initialize, never computed)
        private Vector2Int _gridPosition;
        public Vector2Int GridPosition => _gridPosition;
        
        private SpriteRenderer _renderer;
        private Color _savedColorBeforePreview; // Salva colore prima della preview
        private bool _isShowingPreview; // Flag per tracciare se sta mostrando preview
        
        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                Debug.LogError("Tile does not have a SpriteRenderer component!");
            }
            else
            {
                _renderer.material = new Material(Shader.Find(DefaultShaderName));
                _renderer.color = _normalColor;
                SetState(TileState.Locked); // Inizialmente bloccato
            }

        }
        
        #endregion

        #region Initialization

        /// <summary>
        /// Richiamato all'instanziazione. Salva la posizione in griglia come logica core assoluta.
        /// </summary>
        public void Initialize(Vector2 gridPosition, Vector3 worldPosition, int sortingOrder)
        {
            // Cache grid position as Source of Truth
            _gridPosition = new Vector2Int((int)gridPosition.x, (int)gridPosition.y);
            
            name = $"Tile_{_gridPosition.x}_{_gridPosition.y}";
            transform.position = worldPosition;
            
            // Imposta sorting order per rendering isometrico corretto
            if (_renderer != null)
                _renderer.sortingOrder = sortingOrder;

            // Text sopra il tile per debug coordinate
            if (_coordinatesText != null)
            {
                _coordinatesText.text = $"{_gridPosition.x},{_gridPosition.y}";
                _coordinatesText.sortingOrder = sortingOrder + 1;
            }

            State = TileState.Locked;
        }

        #endregion

        #region Input Interface (IHoverable)

        public void OnHoverEnter()
        {
            if (_isShowingPreview) return;
            _renderer.color = _hoverColor;
        }

        public void OnHoverExit()
        {
            if (_isShowingPreview) return;

            ResetTint();
        }
        
        public void OnClick()
        {
            Debug.Log($"Tile clicked: {name}");
            // Aggiungi qui logica per selezione, pathfinding, ecc
        }

        public void OnRightClick(Vector3 worldPosition)
        {
            // Future: dispatch command (e.g., move unit here)
        }
        
        #endregion
        
        #region State Modification
        
        /// <summary>
        /// Cambia lo stato logico della tile e il corrispettivo colore visivo.
        /// </summary>
        public void SetState(TileState state)
        {
            State = state;
            
            _renderer.color = state switch
            {
                TileState.Locked => _lockedColor,
                TileState.Buyable => _buyableColor,
                TileState.Unlocked => _unlockedColor,
                _ => _renderer.color
            };
        }
        
        /// <summary>
        /// Sblocca la tile se e' attualmente acquistabile.
        /// </summary>
        public void Unlock()
        {
            if (State == TileState.Buyable)
            {
                SetState(TileState.Unlocked);
            }
        }
       
        /// <summary>
        /// Forza momentaneamente il master material tint ad un altro colore (es. hover per edifici validi).
        /// </summary>
        public void PreviewTint(Color color)
        {
            if (_renderer == null) return;
           
            _isShowingPreview = true;
            _renderer.color = color;
        }

        /// <summary>
        /// Ripristina la tinta al suo stato naturale in base al logico TileState.
        /// </summary>
        public void ResetTint()
        {
            if (_renderer == null) return;

            _renderer.color = State switch
            {
                TileState.Locked => _lockedColor,
                TileState.Buyable => _buyableColor,
                TileState.Unlocked => _unlockedColor,
                _ => _normalColor
            };

            _isShowingPreview = false;
        }

        /// <summary>
        /// DEBUG ONLY: Forza colore per pathfinding debug (ignora hover state)
        /// </summary>
        public void DebugSetColor(Color color)
        {
            if (_renderer == null) return;
            _renderer.color = color;
        }
        
        #endregion
    }
    
    
    public enum TileState
    {
        Locked,        // Bloccato
        Buyable,       // Acquistabile
        Unlocked       // Sbloccato
    }
    
}