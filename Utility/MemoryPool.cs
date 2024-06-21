using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VirtualMeshCreator.Utility
{
    public class MemoryPool
    {
        private readonly ConcurrentBag<LinkedListNode<int>> pool;

        public MemoryPool()
        {
            pool = new ConcurrentBag<LinkedListNode<int>>();
        }

        public LinkedListNode<int> RentNode(int value)
        {
            if (pool.TryTake(out var node))
            {
                node.Value = value;
                return node;
            }
            return new LinkedListNode<int>(value);
        }

        public void ReturnNode(LinkedListNode<int> node)
        {
            node.Value = default;
            pool.Add(node);
        }
    }
}
