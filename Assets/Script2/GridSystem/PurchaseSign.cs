using UnityEngine;

namespace Script2.GridSystem
{
    public class PurchaseSign : MonoBehaviour
    {
        private Vector2Int _zoneCoord;

        private Vector3 _originalScale;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _animationSpeed = 5f;
        private bool _hovered;
        
        void Update()
        {
            Vector3 targetScale = _hovered ? _originalScale * _scaleMultiplier : _originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _animationSpeed);
        }

       
        public void Initialize(Vector2Int zoneCoord)
        {
            _zoneCoord = zoneCoord;
            _originalScale = transform.localScale;
        }
        

        void OnMouseDown()
        {
            GridManager.Instance.PurchaseZone(_zoneCoord);
            Destroy(gameObject);
        }
        
        
        void OnMouseEnter()
        {
            _hovered = true;
        }

        void OnMouseExit()
        {
            _hovered = false;
        }
    }
}