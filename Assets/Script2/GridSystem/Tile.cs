using TMPro;
using UnityEngine;

namespace Script2.GridSystem
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _coordinatesText;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor = Color.yellow;
        
        [SerializeField] private Color _lockedColor = Color.grey; //verde grigio
        [SerializeField] private Color _buyableColor = Color.green; //verde verde;
        [SerializeField] private Color _unlockedColor = Color.white;//verde normale;
        
        public TileState State { get; private set; }
        
        private SpriteRenderer _renderer;

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
            name = $"Tile_{gridPosition.x}_{gridPosition.y}";
            transform.position = worldPosition;

            _coordinatesText.text = $"{(int)gridPosition.x},{(int)gridPosition.y}";
            _coordinatesText.sortingOrder = sortingOrder + 1; //sopra il Tile
            _renderer.sortingOrder = sortingOrder;

            State = TileState.Locked;
        }

        void OnMouseEnter()
        {
            _renderer.color = _hoverColor;
            //Debug.Log($"Tile OnMouseEnter: {name}");
        }

        void OnMouseExit()
        {
            _renderer.color = State == TileState.Unlocked ? _normalColor : _lockedColor;
            //Debug.Log($"Tile OnMouseExit: {name}");
        }
        
        void OnMouseDown()
        {
            Debug.Log($"Tile clicked: {name}");
            // Aggiungi qui logica per selezione, pathfinding, ecc
        }

        public void SetState(TileState state)
        {
            State = state;

            switch (state)
            {
                case TileState.Locked:
                    _renderer.color = _lockedColor;
                    break;
                case TileState.Buyable:
                    _renderer.color = _buyableColor;
                    break;
                case TileState.Unlocked:
                    _renderer.color = _unlockedColor;
                    break;
            }
        }
        
        // Funzione per "acquistare" il Tile
        public void Unlock()
        {
            if (State == TileState.Buyable)
            {
                SetState(TileState.Unlocked);
                // Logica per sbloccare tile (ad esempio, aggiungi monete, notifiche, ecc.)
            }
        }

    }
    
    
    public enum TileState
    {
        Locked,        // Bloccato
        Buyable,       // Acquistabile
        Unlocked       // Sbloccato
    }
    
}