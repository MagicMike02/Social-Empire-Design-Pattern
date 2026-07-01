using NUnit.Framework;
using Script.BuildingSystem;
using UnityEngine;

namespace Tests.EditMode.BuildingSystem
{
    public class BuildingPlacementStateTrackerTests
    {
        [Test]
        public void Select_And_Clear_ResetPlacementState()
        {
            var tracker = new BuildingPlacementStateTracker();
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();

            tracker.Select(config);
            tracker.SetCurrentCell(new Vector3Int(4, 5, 0));
            tracker.MarkPreviewState(new Vector3Int(4, 5, 0), false);

            Assert.That(tracker.SelectedConfig, Is.SameAs(config));
            Assert.That(tracker.CurrentCell, Is.EqualTo(new Vector3Int(4, 5, 0)));
            Assert.That(tracker.CanPlaceAtCurrentPosition(), Is.False);
            Assert.That(tracker.ShouldRefreshPreview(new Vector3Int(4, 5, 0), false), Is.False);
            Assert.That(tracker.ShouldRefreshPreview(new Vector3Int(6, 7, 0), false), Is.True);

            tracker.Clear();

            Assert.That(tracker.SelectedConfig, Is.Null);
            Assert.That(tracker.CurrentCell, Is.EqualTo(Vector3Int.zero));
            Assert.That(tracker.CanPlaceAtCurrentPosition(), Is.False);

            Object.DestroyImmediate(config);
        }
    }
}
