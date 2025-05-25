using UnityEngine;

namespace Script.GridSystem
{
    [System.Serializable] //aggiunto per la serializzazione
    public class ChunkData
    {
        public Vector2Int position;
        public CellData[][] cells;

        public ChunkData(Vector2Int position, CellData[][] cells)
        {
            this.position = position;
            this.cells = cells;
        }
    }
}