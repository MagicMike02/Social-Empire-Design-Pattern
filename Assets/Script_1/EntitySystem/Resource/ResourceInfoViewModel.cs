namespace Script.EntitySystem.Resource
{
    public class ResourceInfoViewModel
    {
        private ResourceData selectedResource;

        public void SetSelectedResource(Resource resource)
        {
            if (resource != null)
            {
                selectedResource.id = resource.GetId();
                selectedResource.name = resource.GetResourceName();
                selectedResource.amount = resource.GetAmount();
            }
            else
            {
                selectedResource = new ResourceData();
            }
        }

        public ResourceData GetSelectedResourceData()
        {
            return selectedResource;
        }
    }
}