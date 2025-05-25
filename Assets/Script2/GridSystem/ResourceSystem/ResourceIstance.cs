using UnityEngine;

namespace Script2.GridSystem.ResourceSystem
{
    public class ResourceInstance : MonoBehaviour
    {
        private ResourceDataSO _data;
        private Vector2Int _gridPosition;
        private ResourceManager _manager;

        private SpriteRenderer _sr;

        private int _prefabIndex;

        private void Awake()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            SetSortingOrder();
        }

        public void Initialize(ResourceDataSO data, Vector2Int gridPos, ResourceManager manager)
        {
            _data = data;
            _gridPosition = gridPos;
            _manager = manager;
        }

        void SetSortingOrder()
        {
            if (_sr)
            {
                // Più in basso sull’asse Y -> sortingOrder maggiore -> disegnato sopra
                _sr.sortingOrder = -(int)(transform.position.y * 100);
            }
        }

        private void OnMouseDown()
        {
            CollectResource();
        }

        public void CollectResource()
        {
            // Notifica al manager
            _manager.OnResourceCollected(_gridPosition, _data);
        }
    }
}