﻿﻿using TMPro;
using UnityEngine;
using Script2.InputSystem;

namespace Script2.GridSystem
{
    /// <summary>
    /// Represents a single tile in the isometric grid.
    /// Implements IGridEntity to provide authoritative grid position (no mathematical conversion needed).
    /// Implements IHoverable for input system integration.
    /// </summary>
    public class Tile : MonoBehaviour, IHoverable, Script2.BuildingSystem.IGridEntity
    {
        [SerializeField] private TextMeshPro _coordinatesText;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor = Color.yellow;
        
        [SerializeField] private Color _lockedColor = Color.grey; //verde grigio
        [SerializeField] private Color _buyableColor = Color.grey; //verde verde;
        [SerializeField] private Color _unlockedColor = Color.white;//verde normale;
        
        public TileState State { private set; get; }
        
        // Source of Truth: grid position (cached at Initialize, never computed)
        private Vector2Int _gridPosition;
        public Vector2Int GridPosition => _gridPosition;
        
        private SpriteRenderer _renderer;
        private Color _savedColorBeforePreview; // Salva colore prima della preview
        private bool _isShowingPreview; // Flag per tracciare se sta mostrando preview


        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                Debug.LogError("Tile does not have a SpriteRenderer component!");
            }
            else
            {
                _renderer.material = new Material(Shader.Find("Sprites/Default"));
                _renderer.color = _normalColor;
                SetState(TileState.Locked); // Inizialmente bloccato
            }

        }
        
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
        
        // Funzione per "acquistare" il Tile
        public void Unlock()
        {
            if (State == TileState.Buyable)
            {
                SetState(TileState.Unlocked);
            }
        }
       
        public void PreviewTint(Color color)
        {
            if (_renderer == null) return;
            if (!_isShowingPreview)
            {
                _savedColorBeforePreview = _renderer.color;
            }
            _isShowingPreview = true;
            _renderer.color = color;
        }

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
            // Non settare _isShowingPreview per non bloccare hover
        }
    }
    
    
    public enum TileState
    {
        Locked,        // Bloccato
        Buyable,       // Acquistabile
        Unlocked       // Sbloccato
    }
    
}