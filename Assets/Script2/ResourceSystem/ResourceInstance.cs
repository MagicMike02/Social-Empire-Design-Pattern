using UnityEngine;

namespace Script2.ResourceSystem
{
    public class ResourceInstance : MonoBehaviour
    {
        private ResourceDataSO _data;
        private Vector2Int _gridPosition;
        private ResourceManager _manager;

        private SpriteRenderer _spriteRenderer;

        private int _prefabIndex;
        
        public ResourceDataSO Data => _data;
        

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(ResourceDataSO data, Vector2Int gridPos, ResourceManager manager)
        {
            _data = data;
            _gridPosition = gridPos;
            _manager = manager;
            
            SetSortingOrder();
        }

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

        private void OnMouseDown()
        {
            CollectResource();
        }

        private void CollectResource()
        {
            // _manager.OnResourceCollected(_gridPosition, _data);
            if (_manager != null && _data != null)
                _manager.HandleResourceCollected(_gridPosition, _data);
            else
                Debug.LogWarning("ResourceInstance: Manager o Data non impostati!");
        }

    }
}