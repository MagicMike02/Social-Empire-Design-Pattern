using NUnit.Framework;
using Script.GridSystem;
using UnityEngine;

namespace Tests.EditMode.Grid
{
    public class GridTests
    {
        [Test]
        public void IsoToWorldAndBack_RoundTripsCellCoordinates()
        {
            var grid = new Grid<int>(10, 10, 1f);

            Vector3 world = grid.GetIsoToWorldPosition(3, 4);
            grid.GetWorldToIsoPosition(world, out int x, out int y);

            Assert.That(x, Is.EqualTo(3));
            Assert.That(y, Is.EqualTo(4));
        }

        [Test]
        public void SetValueAndGetValue_ReturnsStoredItem()
        {
            var grid = new Grid<string>(5, 5, 1f);

            grid.SetValue(2, 3, "house");

            Assert.That(grid.GetValue(2, 3), Is.EqualTo("house"));
        }

        [Test]
        public void GetValue_OutOfBounds_ReturnsDefault()
        {
            var grid = new Grid<string>(5, 5, 1f);

            Assert.That(grid.GetValue(-1, 0), Is.Null);
            Assert.That(grid.GetValue(99, 99), Is.Null);
        }

        [Test]
        public void SnapWorldToGridWorld_ReturnsClosestCellCenter()
        {
            var grid = new Grid<int>(10, 10, 1f);
            var snapped = grid.SnapWorldToGridWorld(new Vector3(3.2f, 4.1f, 0f));

            grid.GetWorldToIsoPosition(snapped, out int x, out int y);

            Assert.That(x, Is.EqualTo(3));
            Assert.That(y, Is.EqualTo(4));
        }
    }
}
