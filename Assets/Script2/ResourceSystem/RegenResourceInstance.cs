using UnityEngine;

namespace Script2.ResourceSystem
{
    public class RegenResourceInstance : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
                Debug.LogWarning($"{nameof(SpriteRenderer)} non trovato su {gameObject.name} o sui suoi figli.");

        }

        private void Start()
        {
            SetSortingOrder();
        }

        private void SetSortingOrder()
        {
            if (_spriteRenderer)
            {
                // Usa la stessa logica di calcolo del sortingOrder di ResourceInstance
                // per mantenere la coerenza visiva.
                _spriteRenderer.sortingOrder = -(int)(transform.position.y * 100);
            }
            else
            {
                Debug.LogWarning($"SpriteRenderer non trovato su {gameObject.name} o sui suoi figli per RegenPlaceholderSorting.");
            }
        }

        private void OnMouseDown()
        {
            Debug.Log("Regen resource");
        }
    }
}