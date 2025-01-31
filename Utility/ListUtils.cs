using System.Collections.Generic;

namespace VirtualMeshCreator.Utility
{
    public static class ListUtils
    {
        public static void Reserve<T>(List<T> list, int capacity)
        {
            if(capacity > list.Capacity)
            {
                list.Capacity = capacity;
            }
        }

        public static void Reserve<T>(List<T> list, uint capacity)
        {
            if(capacity > list.Capacity)
            {
                list.Capacity = (int)capacity;
            }
        }

        public static KeyValuePair<TKey, TValue> Find<TKey, TValue>(ref Dictionary<TKey, TValue> dict, TKey key)
        {
            if(dict.TryGetValue(key, out TValue value))
            {
                return new KeyValuePair<TKey, TValue>(key, value);
            }

            return new KeyValuePair<TKey, TValue>();
        }
    }
}
