using System.Collections.Generic;
using System.Linq;
using Script.EntitySystem.Building;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Unit;
using Script.PathFinding;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.GridSystem
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance; // Istanza singleton
        private Cell[][] cells;
        private Dictionary<Vector2Int, Unit> unitDictionary;
        private Dictionary<Vector2Int, Building> buildingDictionary;
        private Dictionary<Vector2Int, Chunk> chunkDictionary;
        // [SerializeField] int chunkWidth = 20;
        // [SerializeField] int chunkHeight = 20;
        [SerializeField] IPathfindingAlgorithm pathfindingAlgorithm;
        [SerializeField] IEventManager eventManager;
        [SerializeField] ICellFactory cellFactory;
        [SerializeField] IEntityFactory entityFactory;

        [SerializeField] public int width { get; } = 100;
        [SerializeField] public int height { get; } = 100;
        [SerializeField] public Vector2 cellSize { get; } = new Vector2(1, 2);
        [SerializeField] public float tileWidth { get; } = 1.28f; // Se 128px, con 100 PPU → 1.28 Unity Units
        [SerializeField] public float tileHeight { get; } = 0.64f;
        [SerializeField] GameObject tilePrefab;

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

        private void Initialize()
        {
            cells = new Cell[width][];
            for (int x = 0; x < width; x++)
            {
                cells[x] = new Cell[height];
                for (int y = 0; y < height; y++)
                {
                    cells[x][y] = cellFactory.CreateCell(new Terrain(Color.green, true));

                    float isoX = (x - y) * tileWidth / 2;
                    float isoY = (x + y) * tileHeight / 2;

                    Vector3 position = new Vector3(isoX, isoY, 0);
                    Instantiate(tilePrefab, position, Quaternion.identity, transform);
                }
            }

            unitDictionary = new Dictionary<Vector2Int, Unit>();
            buildingDictionary = new Dictionary<Vector2Int, Building>();
            chunkDictionary = new Dictionary<Vector2Int, Chunk>();
            if (pathfindingAlgorithm == null)
            {
                pathfindingAlgorithm = new AStarAlgorithm();
            }
        }

        public Cell GetCell(Vector2Int position)
        {
            if (position.x >= 0 && position.x < width && position.y >= 0 && position.y < height)
            {
                return cells[position.x][position.y];
            }
            else
            {
                return null;
            }
        }

        public void SetCell(Vector2Int position, Cell cell)
        {
            if (position.x >= 0 && position.x < width && position.y >= 0 && position.y < height)
            {
                cells[position.x][position.y] = cell;
            }
        }

        public void AddUnit(Unit unit)
        {
            if (!unitDictionary.ContainsKey(unit.GetPosition()))
            {
                unitDictionary.Add(unit.GetPosition(), unit);
                GetCell(unit.GetPosition()).SetEntity(unit);
            }
            else
            {
                Debug.LogError("Unit already exists at position: " + unit.GetPosition());
            }
        }

        public void RemoveUnit(Unit unit)
        {
            if (unitDictionary.ContainsKey(unit.GetPosition()))
            {
                unitDictionary.Remove(unit.GetPosition());
                GetCell(unit.GetPosition()).SetEntity(null);
            }
            else
            {
                Debug.LogError("Unit does not exist at position: " + unit.GetPosition());
            }
        }

        public Unit GetUnit(Vector2Int position)
        {
            unitDictionary.TryGetValue(position, out Unit unit);
            return unit;
        }

        public void AddBuilding(Building building)
        {
            if (!buildingDictionary.ContainsKey(building.GetPosition()))
            {
                buildingDictionary.Add(building.GetPosition(), building);
                GetCell(building.GetPosition()).SetEntity(building);
            }
            else
            {
                Debug.LogError("Building already exists at position: " + building.GetPosition());
            }
        }

        public void RemoveBuilding(Building building)
        {
            if (buildingDictionary.ContainsKey(building.GetPosition()))
            {
                buildingDictionary.Remove(building.GetPosition());
                GetCell(building.GetPosition()).SetEntity(null);
            }
            else
            {
                Debug.LogError("Building does not exist at position: " + building.GetPosition());
            }
        }

        public Building GetBuilding(Vector2Int position)
        {
            buildingDictionary.TryGetValue(position, out Building building);
            return building;
        }

        public List<Unit> GetUnitsInRange(Vector2Int position, float range)
        {
            List<Unit> unitsInRange = new List<Unit>();
            foreach (Unit unit in unitDictionary.Values)
            {
                if (Vector2.Distance(new Vector2(position.x, position.y),
                        new Vector2(unit.GetPosition().x, unit.GetPosition().y)) <= range)
                {
                    unitsInRange.Add(unit);
                }
            }

            return unitsInRange;
        }

        public List<Building> GetBuildingsInRange(Vector2Int position, float range)
        {
            List<Building> buildingsInRange = new List<Building>();
            foreach (Building building in buildingDictionary.Values)
            {
                if (Vector2.Distance(new Vector2(position.x, position.y),
                        new Vector2(building.GetPosition().x, building.GetPosition().y)) <= range)
                {
                    buildingsInRange.Add(building);
                }
            }

            return buildingsInRange;
        }

        public Vector2 GameToScreen(Vector2Int position)
        {
            return new Vector2(position.x * cellSize.x, position.y * cellSize.y);
        }

        public Vector2Int ScreenToGame(Vector2 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / cellSize.x), Mathf.FloorToInt(position.y / cellSize.y));
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, List<Unit> group)
        {
            return pathfindingAlgorithm.FindPath(start, end, group);
        }

        public void UnlockGrid(Vector2Int position)
        {
            GetCell(position).SetUnlocked(true);
        }

        public void GenerateResources(Vector2Int position)
        {
        }

        public void GenerateMonsters(Vector2Int position)
        {
        }

        public bool CanBuild(Building building)
        {
            return true;
        }

        public void Build(Building building)
        {
            AddBuilding(building);
            eventManager.RaiseEvent(new BuildingBuiltEvent(building.GetPosition(), building.buildingType));
        }

        public void Destroy(Building building)
        {
            RemoveBuilding(building);
            eventManager.RaiseEvent(new BuildingDestroyedEvent(building.GetPosition(), building.buildingType));
        }

        public int GetZorder(Vector2Int position)
        {
            return 0;
        }

        public void HandleGridEvent(GridEvent gridEvent)
        {
            Debug.Log("GridEvent handled: " + gridEvent.GetType().Name + " at " + gridEvent.GetPosition());
        }

        public void GenerateTerrain()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var generatedTerrain = new Terrain(Color.green, true);
                    cells[x][y] = cellFactory.CreateCell(generatedTerrain);
                }
            }
        }

        private Chunk GenerateChunk(Vector2Int chunkPosition)
        {
            Chunk chunk = new Chunk(chunkPosition, Mathf.CeilToInt(width / 5), Mathf.CeilToInt(height / 5));
            TerrainGenerator terrainGenerator = new TerrainGenerator(0, null);
            ResourceGenerator resourceGenerator = new ResourceGenerator(0, null);
            EntityPlacer entityPlacer = new EntityPlacer(0f, new List<Vector2Int>());
            chunk.Populate(terrainGenerator, resourceGenerator, entityPlacer);
            chunkDictionary.Add(chunkPosition, chunk);
            return chunk;
        }

        public Chunk LoadChunk(Vector2Int chunkPosition)
        {
            if (chunkDictionary.TryGetValue(chunkPosition, out var loadChunk))
            {
                return loadChunk;
            }
            else
            {
                Chunk chunk = GenerateChunk(chunkPosition);
                chunkDictionary.Add(chunkPosition, chunk);
                return chunk;
            }
        }

        public void UnloadChunk(Vector2Int chunkPosition)
        {
            if (chunkDictionary.ContainsKey(chunkPosition))
            {
                chunkDictionary.Remove(chunkPosition);
            }
        }

        public Zone GetZone(Vector2Int position)
        {
            return GetCell(position).GetZones().FirstOrDefault();
        }

        public void SetZone(Vector2Int position, Zone zone)
        {
            GetCell(position).AddZone(zone);
        }

        public void ExecuteCommand(ICommand command, IEntity entity)
        {
            command.Execute(null);
        }

        public Cell[] GetVisibleCells(Vector2 position, float viewRadius)
        {
            List<Cell> visibleCells = new List<Cell>();
            Vector2Int centerCell = ScreenToGame(position);

            for (int x = centerCell.x - Mathf.FloorToInt(viewRadius);
                 x <= centerCell.x + Mathf.FloorToInt(viewRadius);
                 x++)
            {
                for (int y = centerCell.y - Mathf.FloorToInt(viewRadius);
                     y <= centerCell.y + Mathf.FloorToInt(viewRadius);
                     y++)
                {
                    Vector2Int cellPosition = new Vector2Int(x, y);
                    if (IsPositionValid(cellPosition) &&
                        Vector2.Distance(position, GameToScreen(cellPosition)) <= viewRadius)
                    {
                        visibleCells.Add(GetCell(cellPosition));
                    }
                }
            }

            return visibleCells.ToArray();
        }

        public IEntity[] GetEntitiesInCell(Vector2Int position)
        {
            List<IEntity> entities = new List<IEntity>();
            if (unitDictionary.ContainsKey(position))
            {
                entities.Add(unitDictionary[position]);
            }

            if (buildingDictionary.ContainsKey(position))
            {
                entities.Add(buildingDictionary[position]);
            }

            return entities.ToArray();
        }

        public void ApplyCommand(ICommand command, IEntity entity)
        {
            command.Execute(null);
        }

        public Vector2Int GetGridDimensions()
        {
            return new Vector2Int(width, height);
        }

        public bool IsPositionValid(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }
    }
}