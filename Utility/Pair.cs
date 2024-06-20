using System;

namespace VirtualMeshCreator.Utility
{
    public struct Pair<TKey, TValue>
    {
        //private TKey key;
        //private TValue value;

        public TKey Key;

        public TValue Value;

        public Pair(TKey key, TValue value)
        {
            //this.key = key;
            Key = key;
            //this.value = value;
            Value = value;
        }
    }
}
