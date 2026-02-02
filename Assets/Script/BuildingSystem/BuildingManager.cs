using Script.EconomySystem;
using UnityEngine;
using VContainer;

namespace Script.BuildingSystem
{
    public sealed class BuildingManager : MonoBehaviour
    {
        [SerializeField] private Transform _root;

        private IGridService _grid;
        private GameEconomyManager _economy;
        private BuildingFactory _factory;

        public IGridService Grid => _grid;
        public BuildingFactory Factory => _factory;
        public GameEconomyManager Economy => _economy;
        public Transform Root => _root;

        [Inject]
        public void Construct(GameEconomyManager economy, IGridService grid, BuildingFactory factory)
        {
            _economy = economy;
            _grid = grid;
            _factory = factory;
        }

        private void Awake()
        {
            if (_root == null) _root = transform;
        }
    }
}
