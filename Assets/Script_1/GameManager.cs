using System.Collections.Generic;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Unit;
using Script.GridSystem;
using Script.PathFinding;
using UnityEngine;

namespace Script
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance; // Istanza singleton
        public GridManager gridManager { get; private set; }
        public IEventManager eventManager { get; private set; }
        public IPathfindingAlgorithm pathfindingAlgorithm { get; private set; }
        public ICellFactory cellFactory { get; private set; }
        public IEntityFactory entityFactory { get; private set; }
        public World world { get; private set; } 

        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Mantiene tra le scene, se necessario
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        void Initialize()
        {
            // Inizializza i sistemi principali.
            eventManager ??= new EventManager();
            cellFactory ??= new CellFactory();
            entityFactory ??= new EntityFactory();
            
            world ??= new World(eventManager, pathfindingAlgorithm, cellFactory, entityFactory);
        }

        void Start()
        {
            //Ottengo l'istanza del GridManager nella scena
            gridManager = GridManager.Instance;
        }
        
        public Cell GetCell(Vector2Int position)
        {
            return world.GetCell(position);
        }

        public void SetCell(Vector2Int position, Cell cell)
        {
            world.SetCell(position, cell);
        }

        public void CreateEntity(IEntity entity)
        {
            world.CreateEntity(entity);
        }

        public void DestroyEntity(IEntity entity)
        {
            world.DestroyEntity(entity);
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, List<Unit> group)
        {
            return world.FindPath(start, end, group);
        }

        public void UnlockGrid(Vector2Int position)
        {
            world.UnlockGrid(position);
        }

        //Questi metodi non esistono più in world, quindi li rimuovo
        //public void GenerateResources(Vector2Int position)
        //{
        //    world.GenerateResources(position);
        //}

        //public void GenerateMonsters(Vector2Int position)
        //{
        //    world.GenerateMonsters(position);
        //}

        //public void DestroyBuilding(Building building)
        //{
        //    world.DestroyBuilding(building);
        //}

        //public int GetZorder(Vector2Int position)
        //{
        //    return world.GetZorder(position);
        //}

        //public void HandleGridEvent(GridEvent gridEvent)
        //{
        //    world.HandleGridEvent(gridEvent);
        //}

        public void GenerateTerrain()
        {
            world.GenerateTerrain();
        }

        public Chunk GenerateChunk(Vector2Int chunkPosition)
        {
            return world.GenerateChunk(chunkPosition);
        }

        public Chunk LoadChunk(Vector2Int chunkPosition)
        {
            return world.LoadChunk(chunkPosition);
        }

        public void UnloadChunk(Vector2Int chunkPosition)
        {
            world.UnloadChunk(chunkPosition);
        }

        public void ExecuteCommand(ICommand command, IEntity entity)
        {
            world.ExecuteCommand(command, entity);
        }

        public Cell[] GetVisibleCells(Vector2 position, float viewRadius)
        {
            return world.GetVisibleCells(position, viewRadius);
        }

        public IEntity[] GetEntitiesInCell(Vector2Int position)
        {
            return world.GetEntitiesInCell(position);
        }

        //public void ApplyCommand(ICommand command, IEntity entity)
        //{
        //    world.ApplyCommand(command, entity);
        //}

        public Vector2Int GetGridDimensions()
        {
            return world.GetGridDimensions();
        }

        public bool IsPositionValid(Vector2Int position)
        {
            return world.IsPositionValid(position);
        }
    }
}