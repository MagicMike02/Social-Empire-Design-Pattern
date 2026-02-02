using UnityEngine;

namespace Script.BuildingSystem
{
    /// <summary>
    /// Static utility per operazioni isometriche su SpriteRenderer.
    /// Decoupling: BuildingFactory e BuildingManager non dipendono l'uno dall'altro.
    /// </summary>
    public static class IsometricSortingUtility
    {
        /// <summary>
        /// Applica sorting layer e order a un SpriteRenderer per visualizzazione isometrica corretta.
        /// Formula: sortingOrder = -Y * 100 + baseOrder
        /// </summary>
        public static void ApplySorting(SpriteRenderer renderer, string layer, float yPosition, int baseOrder)
        {
            if (renderer == null) return;
            
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = Mathf.RoundToInt(-yPosition * 100f) + baseOrder;
        }
    }
}
