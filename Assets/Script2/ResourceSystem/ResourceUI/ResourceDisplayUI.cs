using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using Script2.ResourceSystem.Enums;
using Script2.EconomySystem;
using Script2.Core.Events;
using System.Collections.Generic;

namespace Script2.ResourceSystem.ResourceUI
{
    /// <summary>
    /// UI Display per risorse (Gold, Wood, Stone, etc.).
    /// REFACTORED: Dependency Injection per GameEconomyManager (VContainer).
    /// </summary>
    public class ResourceDisplayUI : MonoBehaviour
    {
        [System.Serializable]
        public class ResourceUIElement
        {
            public ResourceType resourceType;
            public Image iconImage;
            public TextMeshProUGUI amountText;
        }

        #region Dependencies (Injected by VContainer)
        
        private GameEconomyManager _economyManager;

        [Inject]
        public void Construct(GameEconomyManager economyManager)
        {
            _economyManager = economyManager;
        }
        
        #endregion

        #region Configuration (Inspector)
        
        [SerializeField] private List<ResourceUIElement> uiElements;
        [SerializeField] private ResourceIconsSO resourceIcons;
        
        #endregion

        private void Start()
        {
            if (_economyManager == null)
            {
                Debug.LogError("[ResourceDisplayUI] GameEconomyManager non iniettato da VContainer!");
                return;
            }
            
            if (resourceIcons == null || uiElements == null)
            {
                Debug.LogError("[ResourceDisplayUI] ResourceIcons o UIElements non assegnati nell'Inspector!");
                return;
            }
            
            // Subscribe a GlobalEventBus
            GlobalEventBus.Subscribe<ResourceAmountChangedEvent>(OnResourceAmountChanged);
            
            // Aggiorna subito la UI con gli importi attuali
            foreach (var element in uiElements)
            {
                UpdateResourceUI(element.resourceType, _economyManager.GetResourceAmount(element.resourceType)); 
            }
        }

        private void OnDestroy()
        {
            // CRITICAL: Unsubscribe per prevenire memory leak
            GlobalEventBus.Unsubscribe<ResourceAmountChangedEvent>(OnResourceAmountChanged);
        }

        // Handler per GlobalEventBus (riceve struct invece di parametri separati)
        private void OnResourceAmountChanged(ResourceAmountChangedEvent evt)
        {
            UpdateResourceUI(evt.Type, evt.CurrentAmount);
        }

        // Il metodo per aggiornare la UI
        private void UpdateResourceUI(ResourceType type, int newAmount)
        {
            foreach (var element in uiElements)
            {
                if (element.resourceType != type) continue;
                if (element.amountText)
                {
                    element.amountText.text = newAmount.ToString();
                }

                if (element.iconImage && resourceIcons)
                {
                    element.iconImage.sprite = resourceIcons.GetIcon(type);
                }
                break;
            }
        }
    }
}