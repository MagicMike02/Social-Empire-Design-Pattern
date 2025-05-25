using System.Collections.Generic;
using Script.EntitySystem.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script
{
    public class BuildingMenuView : MonoBehaviour
    {
        public GameObject buildingButtonPrefab;
        public Transform buttonContainer;
        private Dictionary<BuildingType, GameObject> buildingButtons = new Dictionary<BuildingType, GameObject>();
        private IEventManager eventManager;

        void Awake()
        {
            eventManager = GameManager.Instance.eventManager; //ottenere event manager
        }

        public void InitializeBuildingMenu(List<BuildingType> availableBuildings)
        {
            //crea i bottoni per ogni tipo di edificio disponibile
            foreach (BuildingType buildingType in availableBuildings)
            {
                GameObject buttonGO = Instantiate(buildingButtonPrefab, buttonContainer);
                buttonGO.GetComponent<Button>().onClick.AddListener(() =>
                {
                    eventManager.RaiseEvent(new BuildingSelectedEvent(buildingType));
                });
                TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = buildingType.ToString();
                buildingButtons.Add(buildingType, buttonGO);
            }
        }

        public void EnableBuildingButton(BuildingType buildingType, bool enable)
        {
            //abilita o disabilita un bottone
            if (buildingButtons.ContainsKey(buildingType))
            {
                buildingButtons[buildingType].GetComponent<Button>().interactable = enable;
            }
        }
    }
}