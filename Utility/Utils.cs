using System.Collections.Generic;

namespace VirtualMeshCreator.Utility
{
    public class Utils
    {
        public static bool PairEquals(Pair<uint, uint> a, Pair<uint, uint> b)
        {
            if(a.Key == b.Key && a.Value == b.Value)
                return true;
            return false;
        }

        public static bool KVPairEquals(KeyValuePair<uint, uint> a, KeyValuePair<uint, uint> b)
        {
            if (a.Key == b.Key && a.Value == b.Value)
                return true;
            return false;
        }
    }
}
