using NUnit.Framework;
using Script.ResourceSystem;
using UnityEngine;

namespace Tests.EditMode.ResourceSystem
{
    public class ResourceSpawnOrchestratorTests
    {
        [Test]
        public void TrackAndForgetResource_UpdatesOccupancySnapshot()
        {
            var owner = new GameObject("orchestrator-owner");
            var orchestrator = new ResourceSpawnOrchestrator(null, null, null, null, owner.transform);
            var tracked = new GameObject("tracked-resource");
            var cell = new Vector2Int(2, 3);

            orchestrator.TrackResource(cell, tracked);

            Assert.That(orchestrator.ActiveCount, Is.EqualTo(1));
            Assert.That(orchestrator.HasResourceAt(cell), Is.True);

            orchestrator.ForgetResource(cell);

            Assert.That(orchestrator.ActiveCount, Is.EqualTo(0));
            Assert.That(orchestrator.HasResourceAt(cell), Is.False);

            Object.DestroyImmediate(tracked);
            Object.DestroyImmediate(owner);
        }
    }
}
