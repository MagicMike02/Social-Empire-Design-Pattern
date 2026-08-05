using UnityEngine;

namespace Script.Core
{
    /// <summary>
    /// Centralizzato registry dei layer per il progetto.
    /// Aggiungere nuovi layer qui, usare ovunque via static.
    /// </summary>
    public static class LayerRegistry
    {
        public static readonly int Tile = LayerMask.NameToLayer("Tile");
        public static readonly int Building = LayerMask.NameToLayer("Building");
        public static readonly int Unit = LayerMask.NameToLayer("Unit");
        public static readonly int Resource = LayerMask.NameToLayer("Resource");
        public static readonly int ZoneSign = LayerMask.NameToLayer("ZoneSign");

        // Validazione in editor
        /// <summary>
        /// Trigger automagic pre-sceneload, esegue scan per convalidare string a name Layer mask nel build process.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ValidateLayers()
        {
#if UNITY_EDITOR
            if (Tile == -1) Debug.LogError("[LayerRegistry] Layer 'Tile' non trovato");
            if (Resource == -1) Debug.LogWarning("[LayerRegistry] Layer 'Resource' non trovato");
            if (ZoneSign == -1) Debug.LogWarning("[LayerRegistry] Layer 'ZoneSign' non trovato");
#endif
        }
    }
}
