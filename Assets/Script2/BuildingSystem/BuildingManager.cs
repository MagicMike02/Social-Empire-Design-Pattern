﻿using Script2.EconomySystem;
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

        private void ValidateDependencies()
        {
            if (_factory == null)
            {
                Debug.LogError("[BuildingManager] BuildingFactory non iniettato! VContainer dovrebbe averlo fornito.");
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


    }
}
