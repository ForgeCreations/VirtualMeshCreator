using System.Collections.Generic;

namespace VirtualMeshCreator.Utility
{
    public class LRUCache<TKey, TValue>
    {
        private readonly struct CacheItem
        {
            public TKey Key { get; }
            public TValue Value { get; }

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private readonly int capacity;
        private readonly SortedArray<TKey, LinkedListNode<CacheItem>> cacheMap;
        private readonly LinkedList<CacheItem> cacheList;

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
            cacheMap = new SortedArray<TKey, LinkedListNode<CacheItem>>(capacity);
            cacheList = new LinkedList<CacheItem>();
        }

        public void Add(TKey key, TValue value)
        {
            if(cacheMap.Count >= capacity)
                ShiftCache();

            CacheItem item = new CacheItem(key, value);
            LinkedListNode<CacheItem> node = new LinkedListNode<CacheItem>(item);
            cacheList.AddLast(node);
            cacheMap[key] = node;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            if(cacheMap.TryGetValue(key, out LinkedListNode<CacheItem> node))
            {
                value = node.Value.Value;
                cacheList.Remove(node);
                cacheList.AddLast(node);
            }

            value = default;
            return false;
        }

        private void ShiftCache()
        {
            LinkedListNode<CacheItem> node = cacheList.First;
            cacheList.RemoveFirst();
            cacheMap.Remove(node.Value.Key);
        }
    }
}
