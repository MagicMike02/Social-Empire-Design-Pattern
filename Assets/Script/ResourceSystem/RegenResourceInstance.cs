using UnityEngine;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Placeholder visivo per una risorsa che si sta rigenerando col tempo.
    /// </summary>
    public class RegenResourceInstance : MonoBehaviour
    {
        #region Private Fields
        
        private SpriteRenderer _spriteRenderer;
        
        #endregion

        #region Unity Lifecycle

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
        
        #endregion

        #region Internal Helpers

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

        #endregion

        #region Input Handlers

        private void OnMouseDown()
        {
            Debug.Log("Regen resource");
        }
        #endregion
    }
}