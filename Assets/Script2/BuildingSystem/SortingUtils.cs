﻿using UnityEngine;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Utilità statiche per gestire il sorting order in visualizzazione isometrica.
    /// Gli oggetti più in basso (Y minore) hanno sorting order maggiore (appaiono davanti).
    /// </summary>
    public static class SortingUtils
    {
        /// <summary>
        /// Applica sorting layer e order a un SpriteRenderer per visualizzazione isometrica corretta.
        /// Formula: sortingOrder = -Y * 100 + baseOrder
        /// </summary>
        /// <param name="renderer">SpriteRenderer da configurare</param>
        /// <param name="layer">Nome del sorting layer</param>
        /// <param name="yPosition">Posizione Y world dell'oggetto</param>
        /// <param name="baseOrder">Offset base da aggiungere al sorting order</param>
        public static void ApplySorting(SpriteRenderer renderer, string layer, float yPosition, int baseOrder)
        {
            if (renderer == null) return;
            
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = Mathf.RoundToInt(-yPosition * 100f) + baseOrder;
        }

        /// <summary>
        /// Imposta il colore di un SpriteRenderer con alpha specificata.
        /// Utilizzato per le preview degli edifici (ghost).
        /// </summary>
        /// <param name="renderer">SpriteRenderer da colorare</param>
        /// <param name="color">Colore base</param>
        /// <param name="alpha">Valore alpha (trasparenza) 0-1</param>
        public static void SetGhostColor(SpriteRenderer renderer, Color color, float alpha)
        {
            if (renderer == null) return;
            
            var finalColor = color;
            finalColor.a = Mathf.Clamp01(alpha);
            renderer.color = finalColor;
        }
    }
}
