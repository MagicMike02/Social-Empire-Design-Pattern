using System.Collections.Generic;
using Script2.Economy;
using Script2.ResourceSystem.Enums;
using UnityEngine;

namespace Script2.GridSystem
{
    public class PurchaseSign : MonoBehaviour
    {
        private Vector2Int _zoneCoord;

        private Dictionary<ResourceType, int> _purchaseCost = new();

        private Vector3 _originalScale;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _animationSpeed = 5f;
        private bool _hovered;

        void Update()
        {
            Vector3 targetScale = _hovered ? _originalScale * _scaleMultiplier : _originalScale;
            if (transform.localScale != targetScale)
            {
                transform.localScale =
                    Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _animationSpeed);
            }
        }

        public void Initialize(Vector2Int zoneCoord, Dictionary<ResourceType, int> cost = null)
        {
            _zoneCoord = zoneCoord;
            _originalScale = transform.localScale;
            _purchaseCost = cost;
        }

        public void SetCost(Dictionary<ResourceType, int> cost)
        {
            _purchaseCost = cost;
        }


        void OnMouseDown()
        {
            if (_purchaseCost == null)
            {
                // Se non ci sono costi o il manager non è pronto, sblocca comunque (per testing)
                GridManager.Instance.PurchaseZone(_zoneCoord);
                Debug.Log("TEST: Zona sbloccata senza costi!");
                return;
            }

            var economy = GameEconomyManager.Instance;
            if (economy == null)
            {
                Debug.LogWarning("GameEconomyManager non è disponibile o non è stato inizializzato!");
                return;
            }

            if (economy.CanAfford(_purchaseCost))
            {
                // L'acquisto effettivo e la distruzione del GameObject del segno
                // avverranno all'interno di GridManager.PurchaseZone() dopo la verifica
                GridManager.Instance.PurchaseZone(_zoneCoord);
            }
            else
            {
                Debug.Log("Non hai abbastanza risorse per sbloccare questa zona!");
            }
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