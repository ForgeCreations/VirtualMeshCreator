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
            int temp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = temp;
        }

        public static void Swap(IList<uint> list, int indexA, int indexB)
        {
            uint temp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = temp;
        }

        public static void Swap(ref uint a, ref uint b)
        {
            uint temp = a;
            a = b;
            b = temp;
        }

        public static void Swap(ref Vector3 a, ref Vector3 b)
        {
            Vector3 temp = a;
            a = b;
            b = temp;
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

        public static void Fill<T>(this T[] array, T value)
        {
            if(array == null)
                throw new ArgumentNullException(nameof(array));

            for(int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }
    }
}
