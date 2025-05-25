using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using Script.EntitySystem.Entity;
using UnityEngine;

namespace Script.GridSystem
{
    public class GridView : MonoBehaviour, IEventListener
    {
        private GridData gridData;
        private Vector2 cellSize;
        private Rect worldBounds;
        private IEventManager eventManager;
        public GameObject cellPrefab; //da assegnare tramite l'inspector
        private Dictionary<Vector2Int, GameObject> cellGameObjects = new Dictionary<Vector2Int, GameObject>();

        public GridView(Vector2 cellSize, Rect worldBounds, IEventManager eventManager)
        {
            this.cellSize = cellSize;
            this.worldBounds = worldBounds;
            this.eventManager = eventManager;
            this.eventManager.RegisterListener("CellChangedEvent", this); //ascolta gli eventi
        }

        void Start()
        {
            //inizializza la griglia
            InitializeGrid();
        }

        public void InitializeGrid()
        {
            for (int x = (int)worldBounds.xMin; x < (int)worldBounds.xMax; x++)
            {
                for (int y = (int)worldBounds.yMin; y < (int)worldBounds.yMax; y++)
                {
                    Vector2Int cellPosition = new Vector2Int(x, y);
                    GameObject cellGO = Instantiate(cellPrefab, new Vector3(x * cellSize.x, y * cellSize.y, 0), Quaternion.identity);
                    cellGO.transform.SetParent(this.transform); //rende la gridview il parent
                    cellGameObjects.Add(cellPosition, cellGO);
                }
            }
        }

        // public void DrawGrid(GridData gridData)
        // {
        //     this.gridData = gridData;
        //     foreach (var cellData in gridData.cells)
        //     {
        //         Vector2Int cellPosition = cellData.position;
        //         if(cellGameObjects.ContainsKey(cellPosition))
        //         {
        //             GameObject cellGO = cellGameObjects[cellPosition];
        //             //aggiorna l'aspetto della cella
        //             Image cellImage = cellGO.GetComponent<Image>();
        //             if(cellImage != null)
        //             {
        //                 cellImage.color = GetTerrainColor(cellData.terrain); //usa una funzione per ottenere il colore
        //             }
        //         }
        //     }
        // }

        private Color GetTerrainColor(string terrainName)
        {
            //switch per ottenere il colore del terreno
            switch(terrainName)
            {
                case "Grass": return Color.green;
                case "Water": return Color.blue;
                case "Mountain": return Color.gray;
                default: return Color.white;
            }
        }

        // public void DrawEntities(List<IEntity> entities)
        // {
        //     //implementa la logica per visualizzare le entità
        //     foreach(IEntity entity in entities)
        //     {
        //         Vector2 position = GameManager.Instance.GameToScreen(entity.GetPosition());
        //         //GameObject entityGO = //istanziare prefab entità
        //         //entityGO.transform.position = new Vector3(position.x, position.y, entity.GetZorder());
        //     }
        // }

        // public void HandleMouseClick(Vector2 position)
        // {
        //     eventManager.RaiseEvent(new GridEvent(GameManager.Instance.ScreenToGame(position)));
        // }

        public void SetCellSize(Vector2 cellSize)
        {
            this.cellSize = cellSize;
        }

        public void SetWorldBounds(Rect worldBounds)
        {
            this.worldBounds = worldBounds;
        }

        public void OnEvent(GridEvent eventInstance)
        {
            if (eventInstance is CellChangedEvent)
            {
                CellChangedEvent cellChangedEvent = (CellChangedEvent)eventInstance;
                Vector2Int cellPosition = cellChangedEvent.GetPosition();
                // Aggiorna la visualizzazione della cella specifica
                if (cellGameObjects.TryGetValue(cellPosition, out var cellGo))
                {
                    // Aggiorna l'aspetto della cella (es., colore, sprite)
                    // Image cellImage = cellGo.GetComponent<Image>();
                    // if (cellImage != null)
                    // {
                    //     // Ottieni il colore del terreno dalla cella nel GridManager
                    //     Cell cell = GameManager.Instance.GetCell(cellPosition);
                    //     if (cell != null)
                    //     {
                    //         cellImage.color = cell.GetTerrain().GetColor(); // Usa il colore del terreno
                    //     }
                    //     else
                    //     {
                    //         cellImage.color = Color.white; // Default color
                    //     }
                    // }
                }
            }
        }
    }
}