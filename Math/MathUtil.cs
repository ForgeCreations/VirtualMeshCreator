using System;
using System.Runtime.InteropServices;

namespace VirtualMeshCreator.Math
{
    public static class MathUtils
    {
        // Evil floating point bit level hacking.
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion
        {
            [FieldOffset(0)]
            public float f;

            [FieldOffset(0)]
            public int tmp;
        }

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

        /// <summary>
        /// The Infamous Unusual Fast Inverse Square Root (TM).
        /// </summary>
        public static float InvSqrt(float z)
        {
            if(z == 0) return 0;
            FloatIntUnion u;
            u.tmp = 0;
            float xhalf = 0.5f * z;
            u.f = z;
            u.tmp = 0x5f375a86 - (u.tmp >> 1);
            u.f = u.f * (1.5f - xhalf * u.f * u.f);
            return u.f * z;
        }

        public static float Clamp(float x, float min, float max)
        {
            if(x < min) return min;
            if(x > max) return max;
            return x;
        }

        public static int Clamp(int x, int min, int max)
        {
            if(x < min) return min;
            if(x > max) return max;
            return x;
        }

        public static uint Clamp(uint x, uint min, uint max)
        {
            if(x < min) return min;
            if(x > max) return max;
            return x;
        }
    }
}
