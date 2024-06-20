using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualMeshCreator.Utility
{
    public static class MathUtil
    {
        public static bool IsPowerOfTwo(int value)
        {
            return (value > 0) && ((value & (value - 1)) == 0);
        }

        public static bool IsPowerOfTwo(uint value)
        {
            return (value > 0) && ((value & (value - 1)) == 0);
        }

        public static uint Min3Index(uint A, uint B, uint C) { return (uint)((A < B) ? ((A < C) ? 0 : 2) : ((B < C) ? 1 : 2)); }
        public static uint Min3(uint A, uint B, uint C) { return System.Math.Min(System.Math.Min(A, B), C); }
        public static uint Max3Index(uint A, uint B, uint C) { return (uint)((A > B) ? ((A > C) ? 0 : 2) : ((B > C) ? 1 : 2)); }
        public static uint Max3(uint A, uint B, uint C) { return System.Math.Max(System.Math.Max(A, B), C); }
    }
}
