using UnityEngine;
using UnityEngine.UI;
using TMPro; // Richiede l'importazione di TextMeshPro
using Script2.ResourceSystem.Enums; // Assicurati che il namespace sia corretto
using Script2.Economy; // Assicurati che il namespace del GameEconomyManager sia corretto
using System.Collections.Generic; // Necessario per List

namespace Script2.ResourceSystem.ResourceUI
{
    public class ResourceDisplayUI : MonoBehaviour
    {
        [System.Serializable]
        public class ResourceUIElement
        {
            public ResourceType resourceType;
            public Image iconImage;
            public TextMeshProUGUI amountText;
        }

        [SerializeField] private List<ResourceUIElement> uiElements;
        [SerializeField] private ResourceIconsSO resourceIcons; // Riferimento all'asset ScriptableObject delle icone
        [SerializeField] private GameEconomyManager _economyManager;

        private void Start()
        {
            if (_economyManager == null)
            {
                Debug.LogError("[ResourceDisplayUI] GameEconomyManager non assegnato nell'Inspector! Assegna il riferimento per evitare errori di runtime.");
                return;
            }
            _economyManager.OnResourceAmountChanged += UpdateResourceUI;
            // Aggiorna subito la UI con gli importi attuali all'attivazione
            foreach (var element in uiElements)
            {
                UpdateResourceUI(element.resourceType, _economyManager.GetResourceAmount(element.resourceType)); 
            }
        }

        private void OnDisable()
        {
            if (_economyManager != null)
            {
                _economyManager.OnResourceAmountChanged -= UpdateResourceUI;
            }
        }

        // Il metodo per l'evento OnResourceAmountChanged
        private void UpdateResourceUI(ResourceType type, int newAmount)
        {
            foreach (var element in uiElements)
            {
                if (element.resourceType == type)
                {
                    if (element.amountText != null)
                    {
                        element.amountText.text = newAmount.ToString();
                    }

                    if (element.iconImage != null && resourceIcons != null)
                    {
                        element.iconImage.sprite = resourceIcons.GetIcon(type);
                    }
                    break;
                }
            }
        }
    }
}