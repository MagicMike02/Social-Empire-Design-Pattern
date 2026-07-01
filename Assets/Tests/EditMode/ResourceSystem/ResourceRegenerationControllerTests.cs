using NUnit.Framework;
using Script.ResourceSystem;
using UnityEngine;

namespace Tests.EditMode.ResourceSystem
{
    public class ResourceRegenerationControllerTests
    {
        [Test]
        public void AddAndHasPendingAt_TrackQueuedRegenerations()
        {
            var controller = new ResourceRegenerationController();
            var data = ScriptableObject.CreateInstance<ResourceDataSO>();

            controller.Add(new Vector2Int(3, 4), data, 10f, null);

            Assert.That(controller.Count, Is.EqualTo(1));
            Assert.That(controller.HasPendingAt(new Vector2Int(3, 4)), Is.True);
            Assert.That(controller.HasPendingAt(new Vector2Int(1, 1)), Is.False);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void Tick_WhenTimerExpires_MovesRegenerationToCompletedList()
        {
            var controller = new ResourceRegenerationController();
            var data = ScriptableObject.CreateInstance<ResourceDataSO>();
            var visual = new GameObject("regen-visual");
            var completed = new System.Collections.Generic.List<ResourceRegenerationJob>();

            controller.Add(new Vector2Int(5, 6), data, 1f, visual);
            controller.Tick(1.5f, completed);

            Assert.That(controller.Count, Is.EqualTo(0));
            Assert.That(completed, Has.Count.EqualTo(1));
            Assert.That(completed[0].Position, Is.EqualTo(new Vector2Int(5, 6)));
            Assert.That(completed[0].Data, Is.SameAs(data));
            Assert.That(completed[0].VisualObject, Is.SameAs(visual));

            Object.DestroyImmediate(visual);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Clear_RemovesAllPendingRegenerations()
        {
            var controller = new ResourceRegenerationController();
            var data = ScriptableObject.CreateInstance<ResourceDataSO>();

            controller.Add(new Vector2Int(1, 2), data, 5f, null);
            controller.Clear();

            Assert.That(controller.Count, Is.EqualTo(0));
            Assert.That(controller.HasPendingAt(new Vector2Int(1, 2)), Is.False);

            Object.DestroyImmediate(data);
        }
    }
}
