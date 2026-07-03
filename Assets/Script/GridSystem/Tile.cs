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
        
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        
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
		public Vector3 WorldPosition { get; private set; }

        
        /// <summary>
        /// Ritorna il centro visivo del tile (SpriteRenderer.bounds.center) se disponibile, altrimenti transform.position.
        /// </summary>
        public Vector3 Center => _renderer != null ? _renderer.bounds.center : transform.position;
        
        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock; // Riutilizzato per ogni cambio colore (no GC)
        private bool _isShowingPreview; // Flag per tracciare se sta mostrando preview
        
        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Tile does not have a SpriteRenderer component!");
#endif
                return;
            }

            // MaterialPropertyBlock per-tile: permette di variare il colore senza rompere il batching.
            // Riutilizziamo la stessa istanza per ogni cambio colore (no allocazioni nel hot path).
            _propertyBlock = new MaterialPropertyBlock();

            ApplyColor(_normalColor);
            SetState(TileState.Locked); // Inizialmente bloccato
        }

        void OnDestroy()
        {
            // Il materiale condiviso è ora ownership di TileManager: nessuna distruzione qui.
            // (Risolve race condition: il primo Tile distrutto non rilascia più il materiale degli altri.)
        }
        
        #endregion

        #region Initialization

        /// <summary>
        /// Richiamato all'instanziazione. Salva la posizione in griglia come logica core assoluta.
        /// Il materiale condiviso è fornito da TileManager (ownership centralizzata → no race condition).
        /// </summary>
        public void Initialize(Vector2 gridPosition, Vector3 worldPosition, int sortingOrder, Material sharedMaterial)
        {
            // Cache grid position as Source of Truth
            WorldPosition = worldPosition; 
            _gridPosition = new Vector2Int((int)gridPosition.x, (int)gridPosition.y);
            
			name = $"Tile_{_gridPosition.x}_{_gridPosition.y}";
            transform.position = worldPosition;
            
            // Imposta sorting order per rendering isometrico corretto
            if (_renderer != null)
            {
                _renderer.sharedMaterial = sharedMaterial;
                _renderer.sortingOrder = sortingOrder;
            }

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
            ApplyColor(_hoverColor);
        }

        public void OnHoverExit()
        {
            if (_isShowingPreview) return;

            ResetTint();
        }
        
        public void OnClick()
        {
#if UNITY_EDITOR
            Debug.Log($"Tile clicked: {name}");
#endif
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
            
            ApplyColor(state switch
            {
                TileState.Locked => _lockedColor,
                TileState.Buyable => _buyableColor,
                TileState.Unlocked => _unlockedColor,
                _ => _renderer.color
            });
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
            ApplyColor(color);
        }

        /// <summary>
        /// Ripristina la tinta al suo stato naturale in base al logico TileState.
        /// </summary>
        public void ResetTint()
        {
            if (_renderer == null) return;

            ApplyColor(State switch
            {
                TileState.Locked => _lockedColor,
                TileState.Buyable => _buyableColor,
                TileState.Unlocked => _unlockedColor,
                _ => _normalColor
            });

            _isShowingPreview = false;
        }

        /// <summary>
        /// DEBUG ONLY: Forza colore per pathfinding debug (ignora hover state)
        /// </summary>
        public void DebugSetColor(Color color)
        {
            if (_renderer == null) return;
            ApplyColor(color);
        }
        
        #endregion

        #region Private Helpers

        /// <summary>
        /// Applica il colore tramite MaterialPropertyBlock invece di renderer.color.
        /// Mantiene il materiale condiviso → abilita batching (SRP Batcher / dynamic batching).
        /// </summary>
        private void ApplyColor(Color color)
        {
            if (_renderer == null || _propertyBlock == null) return;

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorPropertyId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
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
