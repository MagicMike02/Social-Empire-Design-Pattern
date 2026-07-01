using NUnit.Framework;
using Script.PathfindingSystem;

namespace Tests.EditMode.Pathfinding
{
    public class BinaryHeapPriorityQueueTests
    {
        [Test]
        public void EnqueueAndDequeue_ReturnsItemsInPriorityOrder()
        {
            var queue = new BinaryHeapPriorityQueue<string>();

            queue.Enqueue("low", 10f);
            queue.Enqueue("high", 1f);
            queue.Enqueue("mid", 5f);

            Assert.That(queue.Dequeue(), Is.EqualTo("high"));
            Assert.That(queue.Dequeue(), Is.EqualTo("mid"));
            Assert.That(queue.Dequeue(), Is.EqualTo("low"));
        }

        [Test]
        public void UpdatePriority_ReordersExistingItem()
        {
            var queue = new BinaryHeapPriorityQueue<string>();

            queue.Enqueue("a", 10f);
            queue.Enqueue("b", 20f);
            queue.UpdatePriority("b", 1f);

            Assert.That(queue.Dequeue(), Is.EqualTo("b"));
            Assert.That(queue.Dequeue(), Is.EqualTo("a"));
        }

        [Test]
        public void ContainsAndClear_WorkAsExpected()
        {
            var queue = new BinaryHeapPriorityQueue<string>();

            queue.Enqueue("item", 3f);

            Assert.That(queue.Contains("item"), Is.True);

            queue.Clear();

            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.Contains("item"), Is.False);
        }

        [Test]
        public void Dequeue_EmptyQueue_ThrowsInvalidOperationException()
        {
            var queue = new BinaryHeapPriorityQueue<string>();

            Assert.Throws<System.InvalidOperationException>(() => queue.Dequeue());
        }
    }
}
