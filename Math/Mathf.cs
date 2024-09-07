using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualMeshCreator.Math
{
    public static class Mathf
    {
        // Optimized for power of two because it relies on division done using bit shifting
        public static uint DivideAndRoundUP(uint Dividend, uint Divisor, uint DivisorAsBitShift)
        {
            return (Dividend + Divisor - 1) >> (int)DivisorAsBitShift;
        }
    }
}
