using UnityEngine;

namespace Script.GridSystem
{
    public class Chunk
    {
        private Cell[][] cells;
        private Vector2Int position;
        private bool isLoaded;
        private int width; 
        private int height; 

        public Chunk(Vector2Int position, int width, int height)
        {
            this.position = position;
            this.isLoaded = false;
            this.width = width;        
            this.height = height;      
            this.cells = new Cell[width][];
            for (int x = 0; x < width; x++)
            {
                this.cells[x] = new Cell[height];
            }
        }

        public void Populate(TerrainGenerator terrainGenerator, ResourceGenerator resourceGenerator, EntityPlacer entityPlacer)
        {
            //genera il terreno
            Terrain[][] terrainGrid = terrainGenerator.GenerateChunkTerrain(position, width, height);

            //popola le celle del chunk
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x][y] = GameManager.Instance.cellFactory.CreateCell(terrainGrid[x][y]); //usa cell factory
                }
            }

            //genera le risorse
            resourceGenerator.GenerateChunkResources(this);

            //posiziona le entità
            entityPlacer.PlaceChunkEntities(this);

            isLoaded = true;
        }
        
        public bool IsLoaded()
        {
            return isLoaded;
        }

        public void SetLoaded(bool loaded)
        {
            isLoaded = loaded;
        }

        public Cell[][] GetCells()
        {
            return cells;
        }

        public void SetCells(Cell[][] cells)
        {
            this.cells = cells;
        }

        public Vector2Int GetPosition()
        {
            return position;
        }

        public int GetWidth() { return width; }
        public int GetHeight() { return height; }
    }
}