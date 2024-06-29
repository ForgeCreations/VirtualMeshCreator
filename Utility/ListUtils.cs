using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualMeshCreator.Utility
{
    public static class ListUtils
    {
        public static void Reserve<T>(List<T> list, int capacity)
        {
            if(capacity > list.Capacity)
            {
                list.Capacity = capacity;
            }
        }

        public static void Reserve<T>(List<T> list, uint capacity)
        {
            if(capacity > list.Capacity)
            {
                list.Capacity = (int)capacity;
            }
        }
    }
}
