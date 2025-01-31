using System;
using System.Collections;
using System.Collections.Generic;

namespace VirtualMeshCreator.Utility
{
    /// <summary>
    /// A more perfermant and efficient alternative to a Dictionary
    /// </summary>
    public class SortedArray<TKey, TValue> : IEnumerable<Pair<TKey, TValue>>
    {
        private Pair<TKey, TValue>[] items;
        private readonly int capacity;
        private int count;

        public int Count => count;

        public TValue this[TKey key]
        {
            get
            {
                int index = FindIndex(key);
                if(index < 0) throw new KeyNotFoundException();
                return items[index].Value;
            }

            set
            {
                int index = FindIndex(key);
                if(index < 0) throw new KeyNotFoundException();
                items[index].Value = value;
            }
        }

        public SortedArray(int capacity = 16)
        {
            items = new Pair<TKey, TValue>[capacity];
            this.capacity = capacity;
            count = 0;
        }

        public void Add(TKey key, TValue value)
        {
            if(count == items.Length) Resize(items.Length << 1);
            int index = FindInsertPosition(key);
            //if(index >= 0 && AreKeysEqual(items[index].Key, key)) return;
            Array.Copy(items, index, items, index + 1, count - index);
            items[index] = new Pair<TKey, TValue>(key, value);
            count++;
        }

        public bool Remove(TKey key)
        {
            int index = FindIndex(key);
            if(index < 0) return false;
            Array.Copy(items, index + 1, items, index, count - index - 1);
            count--;
            if(count > 0 && count == items.Length >> 2) Resize(items.Length >> 1);
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return FindIndex(key) >= 0;
        }

        public Pair<TKey, TValue> FindItem(TKey key)
        {
            int index = FindIndex(key);
            if(index >= 0) return default;
            return items[index];
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = FindIndex(key);
            if(index >= 0)
            {
                value = items[index].Value;
                return true;
            }
            value = default;
            return false;
        }

        private int FindIndex(TKey key)
        {
            int low = 0, high = count - 1;
            while(low <= high)
            {
                int mid = low + (high - low) >> 1;
                TKey midKey = items[mid].Key;

                if(AreKeysEqual(midKey, key)) return mid;
                if(IsKeyLessThan(midKey, key)) low = mid + 1;
                else high = mid - 1;
            }
            return -1;
        }

        private int FindInsertPosition(TKey key)
        {
            int low = 0, high = count;
            while(low < high)
            {
                int mid = low + (high - low) >> 1;
                if(IsKeyLessThan(key, items[mid].Key)) high = mid;
                else low = mid + 1;
            }
            return low;
        }

        private bool IsKeyLessThan(TKey left, TKey right)
        {
            return Comparer<TKey>.Default.Compare(left, right) < 0;
        }

        private bool AreKeysEqual(TKey left, TKey right)
        {
            return EqualityComparer<TKey>.Default.Equals(left, right);
        }

        public void Clear()
        {
            items = new Pair<TKey, TValue>[capacity];
            count = 0;
        }

        private void Resize(int newSize)
        {
            Pair<TKey, TValue>[] newItems = new Pair<TKey, TValue>[(int)newSize];
            Array.Copy(items, newItems, count);
            items = newItems;
        }

        public IEnumerator<Pair<TKey, TValue>> GetEnumerator()
        {
            foreach(Pair<TKey, TValue> pair in items)
            {
                yield return pair;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
