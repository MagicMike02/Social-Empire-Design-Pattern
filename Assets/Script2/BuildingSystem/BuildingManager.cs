using Script2.EconomySystem;
using UnityEngine;
using VContainer;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Coordina BuildingFactory, ResourceManager e GridService per la gestione degli edifici.
    /// REFACTORED: Usa Dependency Injection invece di Singleton pattern.
    /// Punto di accesso centralizzato per le dipendenze del sistema Building.
    /// </summary>
    public sealed class BuildingManager : MonoBehaviour
    {
        [SerializeField] private Transform _root;
        [SerializeField] private BuildingFactory _factory;

        private IGridService _grid;
        private GameEconomyManager _economy;

        [Inject]
        public void Construct(GameEconomyManager economy, IGridService grid)
        {
            _economy = economy;
            _grid = grid;
        }

        private void Awake()
        {
            if (_root == null) _root = transform;
            if (_factory == null) _factory = GetComponent<BuildingFactory>();
        }

        private void ValidateDependencies()
        {
            if (_factory == null)
            {
                Debug.LogError("[BuildingManager] BuildingFactory non trovato! Assegna il componente nell'Inspector o assicurati sia presente sul GameObject.");
            }
            
            if (_economy == null)
            {
                Debug.LogError("[BuildingManager] GameEconomyManager non disponibile! VContainer dovrebbe averlo iniettato.");
            }
            
            if (_grid == null)
            {
                Debug.LogError("[BuildingManager] IGridService non disponibile! VContainer dovrebbe averlo iniettato.");
            }
        }

        public IGridService Grid => _grid;
        public BuildingFactory Factory => _factory;
        public GameEconomyManager Economy => _economy;
        public Transform Root => _root;

        #region SortingUtils (Consolidato)

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

        #endregion
    }
}
