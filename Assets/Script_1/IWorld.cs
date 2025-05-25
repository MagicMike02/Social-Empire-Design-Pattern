using System.Collections.Generic;
using Script.EntitySystem.Building;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Resource;
using Script.EntitySystem.Unit;
using Script.GridSystem;
using UnityEngine;

namespace Script
{
    public interface IWorld
    {
        // Metodi per ottenere e impostare le celle della griglia.
        Cell GetCell(Vector2Int position);
        void SetCell(Vector2Int position, Cell cell);

        // Metodi per la gestione delle entità nel mondo.
        void CreateEntity(IEntity entity);
        void DestroyEntity(IEntity entity);
        void MoveUnit(Unit unit, Vector2Int position, List<Unit> group);
        void AttackUnit(Unit attacker, IEntity target);
        void ApplyDamage(IEntity target, int damage);
        void BuildBuilding(BuildingType buildingType, Vector2Int position);
        void CollectResource(Unit collector, ResourceType resourceType);

        // Metodi per ottenere informazioni sulla griglia e sul mondo.
        Cell[][] GetGrid();
        bool CanBuild(Building building, Vector2Int position);
        Vector2Int GetGridDimensions();
        Vector2 GetCellSize();
        Rect GetWorldBounds();
        bool IsPositionValid(Vector2Int position);
        List<Unit> GetUnits();
        List<Building> GetBuildings();
        List<Resource> GetResources();
        // List<Monster> GetMonsters();
        IEntity[] GetEntitiesInCell(Vector2Int position);
        Resource[] GetResourcesInCell(Vector2Int position);
        Building[] GetBuildingsInCell(Vector2Int position);

        // Metodi per la generazione e gestione del mondo.
        void GenerateTerrain();
        Chunk GenerateChunk(Vector2Int chunkPosition);
        Chunk LoadChunk(Vector2Int chunkPosition);
        void UnloadChunk(Vector2Int chunkPosition);

        // Metodi per l'esecuzione dei comandi.
        void ExecuteCommand(ICommand command, IEntity entity);

         //Metodo per ottenere le celle visibili
        Cell[] GetVisibleCells(Vector2 position, float viewRadius);

        // Metodo per aggiornare la rigenerazione delle risorse
        void UpdateResourceRegeneration();

        // Metodi per la gestione degli eventi
        void OnEvent(GridEvent eventInstance);
        void UnitMoved(UnitMovedEvent eventInstance);
        void ResourceCollected(ResourceCollectedEvent eventInstance);
        void BuildingBuilt(BuildingBuiltEvent eventInstance);
        void BuildingDestroyed(BuildingDestroyedEvent eventInstance);
        // void MonsterSpawned(MonsterSpawnedEvent eventInstance);
        // void MonsterDestroyed(MonsterDestroyedEvent eventInstance);

        //Metodi che reinserisco
        void GenerateResources(Vector2Int position);
        void GenerateMonsters(Vector2Int position);
        void DestroyBuilding(Building building);
        int GetZorder(Vector2Int position);
        void HandleGridEvent(GridEvent gridEvent);
        void ApplyCommand(ICommand command, IEntity entity);

        // Metodo per trovare un percorso nella griglia
        List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, List<Unit> group);
    }
}