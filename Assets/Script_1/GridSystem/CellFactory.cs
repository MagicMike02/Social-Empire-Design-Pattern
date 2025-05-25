using Script.GridSystem;

namespace Script
{
    public class CellFactory : ICellFactory
    {
        public Cell CreateCell(Terrain terrain)
        {
            return new Cell(terrain);
        }
    }
}