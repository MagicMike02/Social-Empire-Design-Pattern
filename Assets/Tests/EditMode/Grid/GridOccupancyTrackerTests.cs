using NUnit.Framework;
using Script.GridSystem;
using UnityEngine;

namespace Tests.EditMode.Grid
{
    public class GridOccupancyTrackerTests
    {
        [Test]
        public void OccupyAndFreeCells_UpdatesCellState()
        {
            var tracker = new GridOccupancyTracker();
            var building = new GameObject("building");

            tracker.OccupyCell(new Vector2Int(2, 3), building);

            Assert.That(tracker.IsCellFree(new Vector2Int(2, 3)), Is.False);

            tracker.FreeCell(new Vector2Int(2, 3));

            Assert.That(tracker.IsCellFree(new Vector2Int(2, 3)), Is.True);

            Object.DestroyImmediate(building);
        }

        [Test]
        public void GetSnapshot_ReturnsCopyOfOccupiedCells()
        {
            var tracker = new GridOccupancyTracker();
            var building = new GameObject("building");

            tracker.OccupyCell(new Vector2Int(1, 1), building);

            var snapshot = tracker.GetSnapshot();

            Assert.That(snapshot.ContainsKey(new Vector2Int(1, 1)), Is.True);
            Assert.That(snapshot[new Vector2Int(1, 1)], Is.SameAs(building));

            Object.DestroyImmediate(building);
        }
    }
}
