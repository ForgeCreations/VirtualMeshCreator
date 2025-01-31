using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VirtualMeshCreator.Utility
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Range : IEnumerable<uint>
    {
        public uint Begin;
        public uint End;

        public Range(uint begin, uint end)
        {
            Begin = begin;
            End = end;
        }

        public IEnumerator<uint> GetEnumerator()
        {
            for(uint i = Begin; i != uint.MaxValue; i = End)
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static bool operator <(Range first, Range other)
        {
            return first.Begin < other.Begin;
        }

        public static bool operator >(Range first, Range other)
        {
            return first.Begin > other.Begin;
        }
    }
}
