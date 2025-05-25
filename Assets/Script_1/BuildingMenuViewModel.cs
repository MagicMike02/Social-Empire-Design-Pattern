using System.Collections.Generic;
using Script.EntitySystem.Building;
using Script.EntitySystem.Resource;
using Script.GridSystem;

namespace Script
{
    public class BuildingMenuViewModel : IEventListener // Implementa IEventListener
{
    private List<BuildingType> availableBuildings = new List<BuildingType>();
    private Dictionary<BuildingType, int> buildingCosts = new Dictionary<BuildingType, int>();
    private IEventManager eventManager;

    public BuildingMenuViewModel(IEventManager eventManager)
    {
        this.eventManager = eventManager;
        //inizializza i dati degli edifici
        availableBuildings.Add(BuildingType.Base);
        availableBuildings.Add(BuildingType.Turret);
        availableBuildings.Add(BuildingType.Factory);
        availableBuildings.Add(BuildingType.ResourceCollector);
        availableBuildings.Add(BuildingType.PowerPlant);

        buildingCosts.Add(BuildingType.Base, 100);
        buildingCosts.Add(BuildingType.Turret, 50);
        buildingCosts.Add(BuildingType.Factory, 200);
        buildingCosts.Add(BuildingType.ResourceCollector, 80);
        buildingCosts.Add(BuildingType.PowerPlant, 120);

        this.eventManager.RegisterListener("ResourceCollectedEvent", this);
    }

    public List<BuildingType> GetAvailableBuildings()
    {
        return availableBuildings;
    }

    public int GetBuildingCost(BuildingType buildingType)
    {
        return buildingCosts[buildingType];
    }

    public void UpdateBuildingAvailability(Dictionary<ResourceType, int> currentResources)
    {
        //aggiorna la disponibilità degli edifici in base alle risorse
        foreach (BuildingType buildingType in availableBuildings)
        {
            int cost = buildingCosts[buildingType];
            bool canAfford = true;
            //controllo semplificato, espandere per gestire più risorse
            if (currentResources.ContainsKey(ResourceType.Metal) && currentResources[ResourceType.Metal] < cost)
            {
                canAfford = false;
            }
            //invia evento per abilitare/disabilitare bottone
            eventManager.RaiseEvent(new BuildingAvailabilityChangedEvent(buildingType, canAfford));
        }
    }

    public void OnEvent(GridEvent eventInstance) // Implementazione di OnEvent
    {
        if (eventInstance is ResourceCollectedEvent)
        {
            //aggiorna disponibilità quando cambiano le risorse
            //esempio di come ottenere le risorse dal world (da adattare)
            Dictionary<ResourceType, int> currentResources = new Dictionary<ResourceType, int>(); //world.GetPlayerResources();
            UpdateBuildingAvailability(currentResources);
        }
    }
}
}