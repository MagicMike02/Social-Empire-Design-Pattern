using System.Collections.Generic;
using JetBrains.Annotations;
using Script2.Economy;
using Script2.ResourceSystem.Enums;
using UnityEngine;

namespace Script2.GridSystem
{
    public class PurchaseSign : MonoBehaviour
    {
        private GameEconomyManager _economyManager;
        private ZoneManager _zoneManager;

        private Dictionary<ResourceType, int> _purchaseCost = new();

        private Vector2Int _zoneCoord;
        private Vector3 _originalScale;
        private bool _hovered;
        
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _animationSpeed = 5f;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                Debug.LogError("PurchaseSign does not have a SpriteRenderer component!");
            }
            else
            {
                _renderer.sortingLayerName = "OnTiles";
                tag = "Resources";
            }
        }

        private void Update()
        {
            Vector3 targetScale = _hovered ? _originalScale * _scaleMultiplier : _originalScale;
            if (transform.localScale != targetScale)
            {
                transform.localScale =
                    Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * _animationSpeed);
            }
        }

        public void Setup(Vector2Int zoneCoord, GameEconomyManager economyManager, ZoneManager zoneManager, [CanBeNull] Dictionary<ResourceType, int> cost = null)
        { 
            _zoneCoord = zoneCoord;
            _economyManager = economyManager;
            _zoneManager = zoneManager;
            _purchaseCost = cost ?? new Dictionary<ResourceType, int>();
            _originalScale = transform.localScale;
        }

        private void OnMouseDown()
        {
            if (_purchaseCost == null)
            {
                _zoneManager.PurchaseZone(_zoneCoord);
                return;
            }

            if (_economyManager == null)
            {
                Debug.LogWarning("GameEconomyManager non è stato assegnato! Chiama Setup() dopo l'instanziazione del prefab.");
                return;
            }

            // Zone con costo
            if (_economyManager.CanAfford(_purchaseCost))
            {
                _zoneManager.PurchaseZone(_zoneCoord);
            }
            else
            {
                Debug.Log("Non hai abbastanza risorse per sbloccare questa zona!");
            }
        }


        private void OnMouseEnter()
        {
            _hovered = true;
        }

        private void OnMouseExit()
        {
            _hovered = false;
        }

       
    }
}