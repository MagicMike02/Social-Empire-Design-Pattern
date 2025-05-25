using Script.EntitySystem.Resource;
using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script.Command
{
    public class CollectResourceCommand : ICommand{
        private Unit collector;
        private ResourceType resourceType;

        public CollectResourceCommand(Unit collector, ResourceType resourceType){
            this.collector = collector;
            this.resourceType = resourceType;
        }
        public bool CanExecute(IWorld world){
            //controlla se ci sono risorse da raccogliere
            Resource[] resources = world.GetResourcesInCell(collector.GetPosition());
            if(resources == null || resources.Length == 0) return false;
            foreach(Resource res in resources){
                if(res.GetResourceType() == resourceType && res.GetAmount() > 0){
                    return true;
                }
            }
            return false;
        }

        public void Execute(IWorld world){
            if(CanExecute(world)){
                world.CollectResource(collector, resourceType);
            }
            else{
                Debug.LogWarning("Cannot execute CollectResourceCommand: No resources to collect.");
            }
        }
    }
}