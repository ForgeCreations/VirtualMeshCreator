using System;
using System.Collections.Generic;

namespace VirtualMeshCreator.Utility
{
    public class Utils
    {
        public static KeyValuePair<uint, uint> Find(Dictionary<uint, uint> dictionary, uint key, KeyValuePair<uint, uint> defaultValue = default)
        {
            foreach(KeyValuePair<uint, uint> kv in dictionary)
            {
                if(kv.Key == key)
                {
                    return kv;
                }
            }
            return defaultValue;
        }

        public static bool KVEquals(KeyValuePair<uint, uint> a, KeyValuePair<uint, uint> b)
        {
            if(a.Key == b.Key && a.Value == b.Value)
                return true;
            return false;
        }
    }
}
