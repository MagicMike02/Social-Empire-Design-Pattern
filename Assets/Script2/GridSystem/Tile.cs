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
        
        public TileState State { private set; get; }
        
        private SpriteRenderer _renderer;
        private Color _savedColorBeforePreview; // Salva colore prima della preview
        private bool _isShowingPreview; // Flag per tracciare se sta mostrando preview

        [SerializeField] private GameObject _buildingInstance;
        public GameObject BuildingInstance => _buildingInstance;

        private bool HasBuilding => _buildingInstance != null;

        public event System.Action<Tile> OnBuildingPlaced;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                Debug.LogError("Tile does not have a SpriteRenderer component!");
            }
            else
            {
                _renderer.color = _normalColor;
                SetState(TileState.Locked); 
                _renderer.sortingLayerName = "Tiles";
                tag = "Tiles";
            }
            
        }
        
        public void Initialize(Vector2 gridPosition, Vector3 worldPosition, int sortingOrder)
        {
            name = $"Tile_{gridPosition.x}_{gridPosition.y}";
            transform.position = worldPosition;
            
            // Imposta sorting order per rendering isometrico corretto
            if (_renderer != null)
            {
                _renderer.sortingOrder = sortingOrder;
            }

            // Text sopra il tile per debug coordinate
            if (_coordinatesText != null)
            {
                _coordinatesText.text = $"{(int)gridPosition.x},{(int)gridPosition.y}";
                _coordinatesText.sortingOrder = sortingOrder + 1;
            }

            State = TileState.Locked;
        }

        void OnMouseEnter()
        {
            if (_isShowingPreview) return;
            _renderer.color = _hoverColor;
        }

        void OnMouseExit()
        {
            if (_isShowingPreview) return;
            _renderer.color = State == TileState.Unlocked ? _normalColor : _lockedColor;
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
            }
        }
        public void PlaceBuilding(GameObject buildingPrefab)
        {
            if (State != TileState.Unlocked || HasBuilding)
                return;
            var t = transform;
            _buildingInstance = Instantiate(buildingPrefab, t.position, Quaternion.identity, t);
            OnBuildingPlaced?.Invoke(this);
        }

        public void AssignBuilding(GameObject building) => _buildingInstance = building;

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
            if (_isShowingPreview)
            {
                _renderer.color = _savedColorBeforePreview;
                _isShowingPreview = false;
            }
            else
            {
                Color targetColor = State switch
                {
                    TileState.Locked => _lockedColor,
                    TileState.Buyable => _buyableColor,
                    TileState.Unlocked => _unlockedColor,
                    _ => _normalColor
                };
                _renderer.color = targetColor;
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