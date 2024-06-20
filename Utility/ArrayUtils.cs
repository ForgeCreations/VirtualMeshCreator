using System;
using System.Collections.Generic;
using System.Linq;
using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.Utility
{
    public static class ArrayUtils
    {
        public static void Swap(IList<int> list, int indexA, int indexB)
        {
            //int temp = list[indexA];
            //list[indexA] = list[indexB]
            //list[indexB] = temp;
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static void Swap(int valA, int valB)
        {
            (valB, valA) = (valA, valB);
        }

        public static void Swap(uint valA, uint valB)
        {
            (valB, valA) = (valA, valB);
        }

        public static void Swap(Vector3 valA, Vector3 valB)
        {
            (valB, valA) = (valA, valB);
        }

        internal static int[] Subtract(uint[] array1, uint[] array2)
        {
            int[] newFinal = new int[array1.Length];
            for(int i = 0; i < array1.Length; i++)
            {
                newFinal[i] = (int)(array1[i] - array2[i]);
            }
            return newFinal;
        }
    }
}
