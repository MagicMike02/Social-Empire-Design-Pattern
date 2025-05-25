namespace Script.GridSystem
{
    public interface ICellFactory
    {
        Cell CreateCell(Terrain terrain);
    }
}