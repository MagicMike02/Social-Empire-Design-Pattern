using System.Collections.Generic;
using NUnit.Framework;
using Script.BuildingSystem;
using Script.PathfindingSystem;
using UnityEngine;

namespace Tests.EditMode.Pathfinding
{
    public class AStarAlgorithmTests
    {
        [Test]
        public void FindPath_WhenStartEqualsGoal_ReturnsSingleCellPath()
        {
            var algorithm = new AStarAlgorithm();
            var grid = new FakeGridService();

            var path = algorithm.FindPath(new Vector2Int(2, 2), new Vector2Int(2, 2), grid);

            Assert.That(path, Has.Count.EqualTo(1));
            Assert.That(path[0], Is.EqualTo(new Vector2Int(2, 2)));
        }

        [Test]
        public void FindPath_WhenGoalBlocked_ReturnsEmptyPath()
        {
            var algorithm = new AStarAlgorithm();
            var grid = new FakeGridService
            {
                BlockedCells = new HashSet<Vector2Int> { new Vector2Int(2, 2) }
            };

            var path = algorithm.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 2), grid);

            Assert.That(path, Is.Empty);
        }

        [Test]
        public void FindPath_OnOpenGrid_ReturnsValidDiagonalPath()
        {
            var algorithm = new AStarAlgorithm();
            var grid = new FakeGridService();

            var path = algorithm.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 2), grid);

            Assert.That(path, Is.EqualTo(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 1),
                new Vector2Int(2, 2)
            }));
        }

        private sealed class FakeGridService : IGridService
        {
            public HashSet<Vector2Int> BlockedCells { get; set; } = new();

            public int Width => 10;
            public int Height => 10;

            public bool TryWorldToCell(Vector3 worldPos, out Vector3Int cell)
            {
                cell = default;
                return false;
            }

            public Vector3 CellToWorld(Vector3Int cell) => Vector3.zero;

            public bool AreCellsFree(Vector3Int originCell, int width, int height) => true;

            public void OccupyCells(Vector3Int originCell, int width, int height, Building building)
            {
            }

            public void FreeCells(Vector3Int originCell, int width, int height)
            {
            }

            public void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid)
            {
            }

            public bool IsCellWalkable(Vector2Int cell)
            {
                return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height && !BlockedCells.Contains(cell);
            }

            public List<Vector2Int> GetWalkableNeighbors(Vector2Int cell)
            {
                var neighbors = new List<Vector2Int>();

                AddIfWalkable(neighbors, new Vector2Int(cell.x + 1, cell.y));
                AddIfWalkable(neighbors, new Vector2Int(cell.x - 1, cell.y));
                AddIfWalkable(neighbors, new Vector2Int(cell.x, cell.y + 1));
                AddIfWalkable(neighbors, new Vector2Int(cell.x, cell.y - 1));
                AddIfWalkable(neighbors, new Vector2Int(cell.x + 1, cell.y + 1));
                AddIfWalkable(neighbors, new Vector2Int(cell.x - 1, cell.y + 1));
                AddIfWalkable(neighbors, new Vector2Int(cell.x + 1, cell.y - 1));
                AddIfWalkable(neighbors, new Vector2Int(cell.x - 1, cell.y - 1));

                return neighbors;
            }

            private void AddIfWalkable(List<Vector2Int> neighbors, Vector2Int cell)
            {
                if (IsCellWalkable(cell))
                {
                    neighbors.Add(cell);
                }
            }
        }
    }
}
