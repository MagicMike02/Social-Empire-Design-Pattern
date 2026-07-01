using UnityEngine;

namespace Script.ResourceSystem
{
    /// <summary>
    /// Contratto minimale per raccogliere una risorsa senza accoppiare ResourceInstance a ResourceManager.
    /// </summary>
    public interface IResourceCollectionHandler
    {
        void HandleResourceCollected(Vector2Int pos, ResourceDataSO data);
    }
}
