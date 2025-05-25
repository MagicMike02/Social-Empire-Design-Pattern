using System.Collections.Generic;
using Script.EntitySystem.Building;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Resource;
using Script.EntitySystem.Unit;
using Script.GridSystem;
using Script.PathFinding;
using UnityEngine;

namespace Script
{
    public class World : IWorld
    {
        private Cell[][] grid;
        private Dictionary<Vector2Int, Unit> unitDictionary = new Dictionary<Vector2Int, Unit>();
        private Dictionary<Vector2Int, Building> buildingDictionary = new Dictionary<Vector2Int, Building>();

        private Dictionary<Vector2Int, List<Resource>>
            resourceDictionary = new Dictionary<Vector2Int, List<Resource>>();

        //private List<Monster> monsters = new List<Monster>();
        private int width;
        private int height;
        private Vector2 cellSize;
        private IEventManager eventManager;
        private IPathfindingAlgorithm pathfindingAlgorithm;
        private ICellFactory cellFactory;
        private IEntityFactory entityFactory;
        private int chunkWidth;
        private int chunkHeight;

        public World(IEventManager eventManager, IPathfindingAlgorithm pathfindingAlgorithm, ICellFactory cellFactory, IEntityFactory entityFactory)
        {
            this.eventManager = eventManager;
            this.pathfindingAlgorithm = pathfindingAlgorithm;
            this.cellFactory = cellFactory;
            this.entityFactory = entityFactory;
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            grid = new Cell[width][];
            for (int x = 0; x < width; x++)
            {
                grid[x] = new Cell[height];
                for (int y = 0; y < height; y++)
                {
                    grid[x][y] = cellFactory.CreateCell(new Terrain(Color.green, true)); // Terreno di default
                }
            }
        }

        public Cell GetCell(Vector2Int position)
        {
            if (position.x >= 0 && position.x < width && position.y >= 0 && position.y < height)
            {
                return grid[position.x][position.y];
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
                grid[position.x][position.y] = cell;
                eventManager.RaiseEvent(new CellChangedEvent(position));
            }
        }

        public void CreateEntity(IEntity entity)
        {
            if (entity is Unit)
            {
                Unit unit = (Unit)entity;
                unitDictionary.Add(entity.GetPosition(), unit);
                GetCell(entity.GetPosition()).SetEntity(entity);
            }
            else if (entity is Building)
            {
                Building building = (Building)entity;
                buildingDictionary.Add(entity.GetPosition(), building);
                GetCell(entity.GetPosition()).SetEntity(entity);
            }
            // else if (entity is Monster)
            // {
            //     Monster monster = (Monster)entity;
            //     monsters.Add(monster);
            //     GetCell(entity.GetPosition()).SetEntity(entity);
            //     eventManager.RaiseEvent(new MonsterSpawnedEvent(entity.GetPosition(), monster));
            // }
            else if (entity is Resource)
            {
                Resource resource = (Resource)entity;
                if (!resourceDictionary.ContainsKey(entity.GetPosition()))
                {
                    resourceDictionary.Add(entity.GetPosition(), new List<Resource>());
                }

                resourceDictionary[entity.GetPosition()].Add(resource);
                GetCell(entity.GetPosition()).SetEntity(entity);
            }
        }

        public void DestroyEntity(IEntity entity)
        {
            if (entity is Unit)
            {
                unitDictionary.Remove(entity.GetPosition());
                GetCell(entity.GetPosition()).SetEntity(null);
            }
            else if (entity is Building)
            {
                Building building = (Building)entity;
                buildingDictionary.Remove(entity.GetPosition());
                GetCell(entity.GetPosition()).SetEntity(null);
                eventManager.RaiseEvent(new BuildingDestroyedEvent(entity.GetPosition(),
                    (entity as Building).buildingType));
            }
            // else if (entity is Monster)
            // {
            //     Monster monster = (Monster)entity;
            //     monsters.Remove(monster);
            //     GetCell(entity.GetPosition()).SetEntity(null);
            //     eventManager.RaiseEvent(new MonsterDestroyedEvent(entity.GetPosition(), monster));
            // }
            else if (entity is Resource)
            {
                Resource resource = (Resource)entity;
                if (resourceDictionary.ContainsKey(entity.GetPosition()))
                {
                    resourceDictionary[entity.GetPosition()].Remove(resource);
                    if (resourceDictionary[entity.GetPosition()].Count == 0)
                    {
                        resourceDictionary.Remove(entity.GetPosition());
                    }
                }

                GetCell(entity.GetPosition()).SetEntity(null);
            }
        }

        public void MoveUnit(Unit unit, Vector2Int position, List<Unit> group)
        {
            Vector2Int oldPosition = unit.GetPosition();
            if (IsPositionValid(position) && GetCell(position).IsWalkable())
            {
                unitDictionary.Remove(oldPosition);
                GetCell(oldPosition).SetEntity(null);
                unit.SetPosition(position);
                unitDictionary.Add(position, unit);
                GetCell(position).SetEntity(unit);
                eventManager.RaiseEvent(new UnitMovedEvent(position, oldPosition));
            }
            else
            {
                Debug.LogWarning("Cannot move unit to " + position);
            }
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, List<Unit> group)
        {
            return pathfindingAlgorithm.FindPath(start, end, group);
        }

        public void UnlockGrid(Vector2Int position)
        {
            GetCell(position).SetUnlocked(true);
        }

        public void AttackUnit(Unit attacker, IEntity target)
        {
            if (target != null && target.GetHealth() > 0)
            {
                int damage = attacker.GetAttackDamage();
                ApplyDamage(target, damage);
            }
        }

        public void ApplyDamage(IEntity target, int damage)
        {
            int newHealth = target.GetHealth() - damage;
            target.SetHealth(newHealth);
            if (newHealth <= 0)
            {
                DestroyEntity(target);
            }
        }

        public void BuildBuilding(BuildingType buildingType, Vector2Int position)
        {
            if (CanBuild(null, position))
            {
                IEntity building = entityFactory.CreateEntity(buildingType.ToString(), position);
                CreateEntity(building);
                eventManager.RaiseEvent(new BuildingBuiltEvent(position, buildingType));
            }
            else
            {
                Debug.LogWarning("Cannot build building at " + position);
            }
        }

        public void CollectResource(Unit collector, ResourceType resourceType)
        {
            List<Resource> cellResources = resourceDictionary[collector.GetPosition()];
            if (cellResources != null)
            {
                foreach (Resource res in cellResources)
                {
                    if (res.GetResourceType() == resourceType && res.GetAmount() > 0)
                    {
                        res.SetAmount(res.GetAmount() - 1);
                        eventManager.RaiseEvent(new ResourceCollectedEvent(collector.GetPosition(), resourceType, 1));
                        if (res.GetAmount() <= 0)
                        {
                            DestroyEntity(res);
                        }

                        return;
                    }
                }
            }
        }

        public Cell[][] GetGrid()
        {
            return grid;
        }

        public bool CanBuild(Building building, Vector2Int position)
        {
            if (!IsPositionValid(position)) return false;
            if (GetCell(position).IsOccupied()) return false;
            return true;
        }

        public void UpdateResourceRegeneration()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Cell cell = grid[x][y];
                    if (cell.GetResource() != ResourceType.None)
                    {
                        float timer = cell.GetResourceRegenerationTimer();
                        timer += Time.deltaTime;
                        if (timer >= 10f)
                        {
                            //rigenera risorsa
                            //aumenta amount risorsa
                            timer = 0f;
                            cell.SetResourceRegenerationTimer(timer);
                        }
                    }
                }
            }
        }

        public void OnEvent(GridEvent eventInstance)
        {
            // Questi eventi devono essere gestiti qui, non in World.
            if (eventInstance is UnitMovedEvent)
            {
                UnitMoved(eventInstance as UnitMovedEvent);
            }
            else if (eventInstance is ResourceCollectedEvent)
            {
                ResourceCollected(eventInstance as ResourceCollectedEvent);
            }
            else if (eventInstance is BuildingBuiltEvent)
            {
                BuildingBuilt(eventInstance as BuildingBuiltEvent);
            }
            else if (eventInstance is BuildingDestroyedEvent)
            {
                BuildingDestroyed(eventInstance as BuildingDestroyedEvent);
            }
            // else if (eventInstance is MonsterSpawnedEvent)
            // {
            //     MonsterSpawned(eventInstance as MonsterSpawnedEvent);
            // }
            // else if (eventInstance is MonsterDestroyedEvent)
            // {
            //     MonsterDestroyed(eventInstance as MonsterDestroyedEvent);
            // }
        }

        public void UnitMoved(UnitMovedEvent eventInstance)
        {
            //gestisci evento
        }

        public void ResourceCollected(ResourceCollectedEvent eventInstance)
        {
            //gestisci evento
        }

        public void BuildingBuilt(BuildingBuiltEvent eventInstance)
        {
            //gestisci evento
        }

        public void BuildingDestroyed(BuildingDestroyedEvent eventInstance)
        {
            //gestisci evento
        }

        // public void MonsterSpawned(MonsterSpawnedEvent eventInstance)
        // {
        //     //gestisci evento
        // }
        //
        // public void MonsterDestroyed(MonsterDestroyedEvent eventInstance)
        // {
        //     //gestisci evento
        // }

        public Resource[] GetResourcesInCell(Vector2Int position)
        {
            if (resourceDictionary.ContainsKey(position))
            {
                return resourceDictionary[position].ToArray();
            }

            return null;
        }

        public Building[] GetBuildingsInCell(Vector2Int position)
        {
            if (buildingDictionary.ContainsKey(position))
            {
                return new Building[] { buildingDictionary[position] };
            }

            return null;
        }

        public Vector2Int GetGridDimensions()
        {
            return new Vector2Int(width, height);
        }

        public List<Unit> GetUnits()
        {
            return new List<Unit>(unitDictionary.Values);
        }

        public List<Building> GetBuildings()
        {
            return new List<Building>(buildingDictionary.Values);
        }

        public List<Resource> GetResources()
        {
            List<Resource> allResources = new List<Resource>();
            foreach (List<Resource> resList in resourceDictionary.Values)
            {
                allResources.AddRange(resList);
            }

            return allResources;
        }

        // public List<Monster> GetMonsters()
        // {
        //     return monsters;
        // }

        public Vector2 GetCellSize()
        {
            return cellSize;
        }

        public Rect GetWorldBounds()
        {
            return new Rect(0, 0, width * cellSize.x, height * cellSize.y);
        }

        public bool IsPositionValid(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        public void GenerateTerrain()
        {
            // Implementazione Placeholder: genera il terreno usando TerrainGenerator
            TerrainGenerator
                terrainGenerator =
                    new TerrainGenerator(0, new NoiseGenerator()); // Passa il seed e il generatore di rumore.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Qui dovresti ottenere il terreno generato per la specifica posizione x, y
                    Terrain terrain =
                        terrainGenerator.GenerateChunkTerrain(new Vector2Int(0, 0), width,
                            height)[x][y]; // Ottieni il terreno per la cella
                    grid[x][y].SetTerrain(terrain); // Imposta il terreno per la cella
                }
            }
        }

        public Chunk GenerateChunk(Vector2Int chunkPosition)
        {
            // Calcola le coordinate globali del chunk
            int chunkStartX = chunkPosition.x * chunkWidth;
            int chunkStartY = chunkPosition.y * chunkHeight;

            // Crea un nuovo chunk
            Chunk chunk = new Chunk(chunkPosition, chunkWidth, chunkHeight);
            Cell[][] chunkCells = new Cell[chunkWidth][];

            // Popola il chunk con le celle dalla griglia globale
            for (int x = 0; x < chunkWidth; x++)
            {
                chunkCells[x] = new Cell[chunkHeight];
                for (int y = 0; y < chunkHeight; y++)
                {
                    int globalX = chunkStartX + x;
                    int globalY = chunkStartY + y;

                    // Assicurati che le coordinate globali siano valide
                    if (globalX >= 0 && globalX < width && globalY >= 0 && globalY < height)
                    {
                        chunkCells[x][y] = grid[globalX][globalY];
                    }
                    else
                    {
                        // Gestisci il caso in cui le coordinate sono fuori dai limiti (opzionale)
                        chunkCells[x][y] =
                            cellFactory.CreateCell(new Terrain(Color.magenta, false)); // Crea una cella vuota o un terreno speciale
                    }
                }
            }

            chunk.SetCells(chunkCells);
            return chunk;
        }

        public Chunk LoadChunk(Vector2Int chunkPosition)
        {
            // ChunkDataSaver dataSaver = new ChunkDataSaver();
            // ChunkData chunkData = dataSaver.LoadChunkData(chunkPosition);
            // if (chunkData == null) return null;
            //
            // // Calcola le coordinate globali del chunk
            // int chunkStartX = chunkPosition.x * chunkWidth;
            // int chunkStartY = chunkPosition.y * chunkHeight;
            //
            // // Crea un nuovo chunk
            // Chunk chunk = new Chunk(chunkPosition, chunkWidth, chunkHeight);
            // Cell[][] chunkCells = new Cell[chunkWidth][];
            //
            // for (int x = 0; x < chunkWidth; x++)
            // {
            //     chunkCells[x] = new Cell[chunkHeight];
            //     for (int y = 0; y < chunkHeight; y++)
            //     {
            //         int globalX = chunkStartX + x;
            //         int globalY = chunkStartY + y;
            //         if (globalX >= 0 && globalX < width && globalY >= 0 && globalY < height)
            //         {
            //             Cell cell = cellFactory.CreateCell(new Terrain(Color.green, true));
            //             cell.SetTerrain(new Terrain(Color.green, true));
            //             chunkCells[x][y] = cell;
            //         }
            //         else
            //         {
            //             chunkCells[x][y] = cellFactory.CreateCell(new Terrain(Color.magenta, false));
            //         }
            //     }
            // }
            //
            // chunk.SetCells(chunkCells);
            // return chunk;
            return new Chunk(new Vector2Int(0,0),0,0);
        }

        public void UnloadChunk(Vector2Int chunkPosition)
        {
            // Implementazione Placeholder: scarica i dati del chunk e rimuovilo dalla memoria
            // In un gioco reale, dovresti salvare il chunk su disco e rimuoverlo dalla griglia.
            Debug.Log("Unloading chunk: " + chunkPosition);
        }
        
        public void ExecuteCommand(ICommand command, IEntity entity)
        {
            if (command.CanExecute(this))
            {
                command.Execute(this);
            }
            else
            {
                Debug.LogWarning("Command cannot be executed.");
            }
        }

        public Cell[] GetVisibleCells(Vector2 position, float viewRadius)
        {
            // Implementazione Placeholder: restituisce le celle visibili entro il raggio dato
            // Usa algoritmi di raycasting o field of view per determinare la visibilità.
            List<Cell> visibleCells = new List<Cell>();
            int centerX = Mathf.FloorToInt(position.x);
            int centerY = Mathf.FloorToInt(position.y);
            for (int x = centerX - (int)viewRadius; x <= centerX + (int)viewRadius; x++)
            {
                for (int y = centerY - (int)viewRadius; y <= centerY + (int)viewRadius; y++)
                {
                    if (Vector2.Distance(position, new Vector2(x, y)) <= viewRadius)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            visibleCells.Add(grid[x][y]);
                        }
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

            if (resourceDictionary.ContainsKey(position))
            {
                entities.AddRange(resourceDictionary[position]);
            }

            return entities.ToArray();
        }

        //public void ApplyCommand(ICommand command, IEntity entity)
        //{
        //    command.Execute(this);
        //}

        //Implementazione dei metodi mancanti
        public void GenerateResources(Vector2Int position)
        {
            // Implementazione della generazione di risorse in una cella specifica
            // Puoi usare un ResourceGenerator o logica inline
            Debug.Log("Generating resources at " + position);
        }

        public void GenerateMonsters(Vector2Int position)
        {
            // Implementazione della generazione di mostri in una cella specifica
            // Usa EntityFactory o logica inline
            Debug.Log("Generating monsters at " + position);
        }

        public void DestroyBuilding(Building building)
        {
            // Implementazione della distruzione di un edificio
            if (building != null)
            {
                DestroyEntity(building);
            }
            else
            {
                Debug.LogWarning("Building to destroy is null");
            }
        }

        public int GetZorder(Vector2Int position)
        {
            // Implementazione per ottenere lo Z-order di una cella
            // Lo Z-order determina l'ordine di rendering delle entità
            if (IsPositionValid(position))
            {
                // Esempio: edifici sopra unità, unità sopra risorse
                if (buildingDictionary.ContainsKey(position)) return 2;
                else if (unitDictionary.ContainsKey(position)) return 1;
                else if (resourceDictionary.ContainsKey(position)) return 0;
                else return 0; //default
            }
            else
            {
                return -1; //posizione non valida
            }
        }

        public void HandleGridEvent(GridEvent gridEvent)
        {
            // Implementazione della gestione degli eventi della griglia
            // Chiamare i metodi OnEvent specifici in base al tipo di evento
            OnEvent(gridEvent);
        }

        public void ApplyCommand(ICommand command, IEntity entity)
        {
            command.Execute(this);
        }
    }
}