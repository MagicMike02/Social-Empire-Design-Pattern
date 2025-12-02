using System.Collections.Generic;
using JetBrains.Annotations;
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
        private bool _hovered;
        
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _animationSpeed = 5f;        
        private GameEconomyManager _economyManager;
        
        private void Update()
        {
            Vector3 targetScale = _hovered ? _originalScale * _scaleMultiplier : _originalScale;
            if (transform.localScale != targetScale)
            {
                transform.localScale =
                    Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _animationSpeed);
            }
        }

        public void Setup(Vector2Int zoneCoord, GameEconomyManager economyManager, [CanBeNull] Dictionary<ResourceType, int> cost = null)
        { 
            _zoneCoord = zoneCoord;
            _economyManager = economyManager;
            _purchaseCost = cost ?? new Dictionary<ResourceType, int>();
            _originalScale = transform.localScale;
        }

        void OnMouseDown()
        {
            if (_purchaseCost == null)
            {
                GridManager.Instance.PurchaseZone(_zoneCoord);
                Debug.Log("TEST: Zona sbloccata senza costi!");
                return;
            }

            var economy = _economyManager;
            if (economy == null)
            {
                Debug.LogWarning("GameEconomyManager non è stato assegnato! Chiama Setup() dopo l'instanziazione del prefab.");
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