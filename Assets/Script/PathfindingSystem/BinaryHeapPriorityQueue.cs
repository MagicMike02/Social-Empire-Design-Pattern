using System;
using System.Collections.Generic;

namespace Script.PathfindingSystem
{
    /// <summary>
    /// Binary Heap based Priority Queue - O(log n) operations.
    /// </summary>
    public class BinaryHeapPriorityQueue<T> where T : IEquatable<T>
    {
        private class Node : IComparable<Node>
        {
            public T Item;
            public float Priority;

            public Node(T item, float priority)
            {
                Item = item;
                Priority = priority;
            }

            public int CompareTo(Node other)
            {
                return Priority.CompareTo(other.Priority);
            }
        }

        private readonly List<Node> _heap = new();
        private readonly Dictionary<T, int> _itemToIndex = new(); // O(1) lookup

        public int Count => _heap.Count;

        /// <summary>
        /// Aggiunge elemento con priorità. O(log n)
        /// </summary>
        public void Enqueue(T item, float priority)
        {
            var node = new Node(item, priority);
            int index = _heap.Count;
            _heap.Add(node);
            _itemToIndex[item] = index;
            BubbleUp(index);
        }

        /// <summary>
        /// Estrae elemento con priorità minima. O(log n)
        /// </summary>
        public T Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("Priority queue is empty");

            var root = _heap[0];
            T result = root.Item;

            // Sposta ultimo elemento alla root
            var lastNode = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);
            _itemToIndex.Remove(result);

            if (_heap.Count > 0)
            {
                _heap[0] = lastNode;
                _itemToIndex[lastNode.Item] = 0;
                BubbleDown(0);
            }

            return result;
        }

        /// <summary>
        /// Verifica se elemento è nella queue. O(1)
        /// </summary>
        public bool Contains(T item)
        {
            return _itemToIndex.ContainsKey(item);
        }

        /// <summary>
        /// Aggiorna priorità di elemento esistente. O(log n)
        /// </summary>
        public void UpdatePriority(T item, float newPriority)
        {
            if (!_itemToIndex.TryGetValue(item, out int index))
                throw new KeyNotFoundException("Item not in queue");

            float oldPriority = _heap[index].Priority;
            _heap[index].Priority = newPriority;

            if (newPriority < oldPriority)
                BubbleUp(index);
            else if (newPriority > oldPriority)
                BubbleDown(index);
        }

        /// <summary>
        /// Pulisce la queue
        /// </summary>
        public void Clear()
        {
            _heap.Clear();
            _itemToIndex.Clear();
        }

        // ========== INTERNAL HEAP OPERATIONS ==========

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_heap[index].CompareTo(_heap[parentIndex]) >= 0)
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int smallest = index;
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;

                if (leftChild < _heap.Count && _heap[leftChild].CompareTo(_heap[smallest]) < 0)
                    smallest = leftChild;

                if (rightChild < _heap.Count && _heap[rightChild].CompareTo(_heap[smallest]) < 0)
                    smallest = rightChild;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
            _itemToIndex[_heap[a].Item] = a;
            _itemToIndex[_heap[b].Item] = b;
        }
    }
}
