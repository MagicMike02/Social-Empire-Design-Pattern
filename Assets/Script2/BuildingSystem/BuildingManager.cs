﻿using Script2.Economy;
using UnityEngine;
using Script2.GridSystem;

namespace Script2.BuildingSystem
{
    /// <summary>
    /// Coordina BuildingFactory, ResourceManager e GridService per la gestione degli edifici.
    /// Punto di accesso centralizzato per le dipendenze del sistema Building.
    /// </summary>
    public sealed class BuildingManager : MonoBehaviour
    {
        [SerializeField] private Transform _root;
        [SerializeField] private BuildingFactory _factory;

        private IGridService _grid;
        private GameEconomyManager _economy;

        private void Awake()
        {
            if (_root == null) _root = transform;
            if (_factory == null) _factory = GetComponent<BuildingFactory>();
            
            // Usa Singleton pattern per accesso coerente
            _economy = GameEconomyManager.Instance;
            _grid = GridManager.Instance;
            
            ValidateDependencies();
        }

        private void ValidateDependencies()
        {
            if (_factory == null)
            {
                Debug.LogError("[BuildingManager] BuildingFactory non trovato! Assegna il componente nell'Inspector o assicurati sia presente sul GameObject.");
            }
            
            if (_economy == null)
            {
                Debug.LogError("[BuildingManager] ResourceManager non disponibile! Assicurati che ResourceManager.Instance sia inizializzato prima di BuildingManager.");
            }
            
            if (_grid == null)
            {
                Debug.LogError("[BuildingManager] IGridService non disponibile! Assicurati che GridManager.Instance sia presente in scena.");
            }
        }

        public IGridService Grid => _grid;
        public BuildingFactory Factory => _factory;
        public GameEconomyManager Economy => _economy;
        public Transform Root => _root;
    }
}
