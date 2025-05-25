using System.Collections.Generic;
using Script.EntitySystem.Building;
using Script.EntitySystem.Entity;
using Script.EntitySystem.Resource;
using Script.EntitySystem.Unit;
using UnityEngine;

namespace Script.GridSystem
{
    public class GridViewModel
    {
        private GridData gridData;
        private Vector2Int selectedCell;
        private IEventManager eventManager;

        public GridViewModel(IEventManager eventManager)
        {
            this.eventManager = eventManager;
        }

        public void UpdateGridData(Cell[][] cells)
        {
            //converte l'array di celle in grid data
            gridData = new GridData();
            gridData.cells = new CellData[cells.Length * cells[0].Length];
            int index = 0;
            for(int x = 0; x < cells.Length; x++)
            {
                for(int y = 0; y < cells[x].Length; y++)
                {
                    Cell cell = cells[x][y];
                    string resourceName = cell.GetResource().ToString();
                    EntityData entityData = null;
                    if (cell.GetEntity() != null)
                    {
                        string entityType = cell.GetEntity().GetType().Name;
                        Dictionary<string, object> properties = new Dictionary<string, object>();
                        // Popola il dizionario delle proprietà con i dati dell'entità.
                        if (cell.GetEntity() is Unit unit)
                        {
                            properties.Add("id", unit.GetId());
                            properties.Add("position", unit.GetPosition());
                            properties.Add("speed", unit.GetSpeed());
                            properties.Add("attackDamage", unit.GetAttackDamage());
                            properties.Add("attackRange", unit.GetAttackRange());
                            // properties.Add("level", unit.GetLevel());
                            properties.Add("health", unit.GetHealth());
                            properties.Add("maxHealth", unit.GetMaxHealth());
                        }
                        else if (cell.GetEntity() is Building building)
                        {
                            properties.Add("id", building.GetId());
                            properties.Add("position", building.GetPosition());
                            properties.Add("width", building.GetWidth());
                            properties.Add("height", building.GetHeight());
                            // properties.Add("buildingType", building.GetBuildingType());
                            properties.Add("health", building.GetHealth());
                            properties.Add("maxHealth", building.GetMaxHealth());
                        }
                        else if (cell.GetEntity() is Resource resource)
                        {
                            properties.Add("id", resource.GetId());
                            properties.Add("position", resource.GetPosition());
                            properties.Add("resourceType", resource.GetResourceType());
                            properties.Add("amount", resource.GetAmount());
                        }
                        // else if (cell.GetEntity() is Monster monster)
                        // {
                        //     properties.Add("id", monster.GetId());
                        //     properties.Add("position", monster.GetPosition());
                        //     properties.Add("attackDamage", monster.GetAttackDamage());
                        //     properties.Add("attackRange", monster.GetAttackRange());
                        //     properties.Add("speed", monster.GetSpeed());
                        //     properties.Add("health", monster.GetHealth());
                        //     properties.Add("maxHealth", monster.GetMaxHealth());
                        // }
                        entityData = new EntityData(entityType, properties);
                    }
                    // gridData.cells[index] = new CellData(terrainName, resourceName, entityData);
                    gridData.cells[index] = new CellData(resourceName, entityData);
                    index++;
                }
            }
        }

        // public CellData GetCellData(Vector2Int position)
        // {
        //     //ottiene i dati della cella alla posizione data
        //     foreach (CellData cellData in gridData.cells)
        //     {
        //         if (cellData.position == position)
        //         {
        //             return cellData;
        //         }
        //     }
        //     return null;
        // }

        // public void HandleCellClick(Vector2 position)
        // {
        //     selectedCell = GameManager.Instance.ScreenToGame(position);
        //     RaiseCellClickedEvent(selectedCell);
        // }

        public Vector2Int GetSelectedCell()
        {
            return selectedCell;
        }

        public void RaiseCellClickedEvent(Vector2Int position)
        {
            eventManager.RaiseEvent(new CellChangedEvent(position));
        }
    }
}