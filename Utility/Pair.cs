using System;
using System.Runtime.InteropServices;

namespace VirtualMeshCreator.Utility
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Pair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
