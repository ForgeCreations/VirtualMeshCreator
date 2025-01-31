using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.DataStructures
{
    public class PairedArray<TKey, TValue>
    {
        private List<Pair<TKey, TValue>> elements;

        public PairedArray()
        {
            elements = new List<Pair<TKey, TValue>>();
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value = default;
                foreach(Pair<TKey, TValue> pair in elements)
                {
                    if(pair.Key.Equals(key))
                    {
                        value = pair.Value;
                        break;
                    }
                }
                return value;
            }

            set
            {
                Pair<TKey, TValue> pairRef = new Pair<TKey, TValue>();
                foreach(Pair<TKey, TValue> pair in elements)
                {
                    if(pair.Key.Equals(key))
                    {
                        pairRef = pair;
                        break;
                    }
                }
                pairRef.Value = value;
            }
        }

        public PairedArray(int capacity)
        {
            elements = new List<Pair<TKey, TValue>>(capacity);
        }

        public void Add(TKey key, TValue value)
        {
            elements.Add(new Pair<TKey, TValue>(key, value));
        }

        public bool ContainsKey(TKey key)
        {
            bool containsKey = false;
            foreach(Pair<TKey, TValue> pair in elements)
            {
                if(pair.Key.Equals(key))
                {
                    containsKey = true;
                    break;
                }
            }
            return containsKey;
        }
    }
}
