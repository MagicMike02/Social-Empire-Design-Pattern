using UnityEngine;

namespace Script2.BuildingSystem
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

        /// <summary>
        /// Imposta il colore di un SpriteRenderer con alpha specificata.
        /// Utilizzato per le preview degli edifici.
        /// </summary>
        public static void SetGhostColor(SpriteRenderer renderer, Color color, float alpha)
        {
            if (renderer == null) return;
            
            var finalColor = color;
            finalColor.a = Mathf.Clamp01(alpha);
            renderer.color = finalColor;
        }
    }
}
