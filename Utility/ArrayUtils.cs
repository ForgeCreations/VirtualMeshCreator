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
            (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
        }

        public static void Swap(IList<uint> list, int indexA, int indexB)
        {
            (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
        }

        public static void Swap(ref uint a, ref uint b)
        {
            (b, a) = (a, b);
        }

        internal static uint[] Add(this IList<uint> arr, uint[] vals)
        {
            uint[] temp = arr.ToArray();
            for(int i = 0; i < arr.Count - 1; i++)
            {
                for(int j = 0; i < vals.Length; j++)
                {
                    temp[i] = arr[i] + vals[i];
                }
            }
            return temp;
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

        internal static uint[] Multiply(this IList<uint> arr, uint val)
        {
            uint[] temp = arr.ToArray();
            for(int i = 0; i < arr.Count() - 1; i++)
            {
                temp[i] = arr[i] * val;
            }
            return temp;
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
