using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.Utility
{
    public static class MeshUtility
    {
        static uint Murmur32(List<uint> InitList)
        {
            uint Hash = 0;
            for(int i = 0; i < InitList.Count; i++)
            {
                //Murmur Add
                uint Element = InitList[i];
                Element *= 0xcc9e2d51;
                Element = (Element << 15) | (Element >> (32 - 15));
                Element *= 0x1b873593;

                //Murmur Mix
                Hash ^= Element;
                Hash = (Hash << 13) | (Hash >> (32 - 13));
                Hash = Hash * 5 + 0xe6546b64;
            }

            return MurmurFinalize32(Hash);
        }

        public static uint MurmurFinalize32(uint hash)
        {
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;
            return hash;
        }

        public static ulong MurmurFinalize64(ulong hash)
        {
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed55accdu;
            hash ^= hash >> 33;
            hash *= 0xc4ceb9fe1a85ec53u;
            hash ^= hash >> 33;
            return hash;
        }

        public static uint MurmurAdd(uint hash, uint element)
        {
            element *= 0xcc9e2d51;
            element = (element << 15) | (element >> (32 - 15));
            element *= 0x1b873593;

            hash ^= element;
            hash = (hash << 13) | (hash >> (32 - 13));
            hash = hash * 5 + 0xe6546b64;
            return hash;
        }

        public static uint MurmurMix(uint hash)
        {
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;
            return hash;
        }

        public static uint LowerNearest2Power(uint x)
        {
            while((x & (x - 1)) != 0)
            {
                x ^= (x & (uint)-x);
            }
            return x;
        }

        public static uint UpperNearest2Power(uint x)
        {
            if((x & (x - 1)) != 0)
            {
                while((x & (x - 1)) != 0)
                {
                    x ^= (x & (uint)-x);
                }
                return x == 0 ? 1u : (x << 1);
            }

            else
            {
                return x == 0 ? 1u : (x << 1);
            }
        }

        public static uint Hash(Vector3 v)
        {
            return (uint)((int)v.x * 73856093 ^ (int)v.y * 19349663 ^ (int)v.z * 83492791);
        }

        public static uint Hash(KeyValuePair<Vector3, Vector3> e)
        {
            uint h0 = Hash(e.Key);
            uint h1 = Hash(e.Value);
            return MurmurMix(MurmurAdd(h0, h1));
        }

        public static uint Hash(Pair<Vector3, Vector3> e)
        {
            uint h0 = Hash(e.Key);
            uint h1 = Hash(e.Value);
            return MurmurMix(MurmurAdd(h0, h1));
        }

        public static uint Hash((Vector3, Vector3) e)
        {
            uint h0 = Hash(e.Item1);
            uint h1 = Hash(e.Item2);
            return MurmurMix(MurmurAdd(h0, h1));
        }

        public static uint Cycle3(uint i)
        {
            uint imod3 = i % 3;
            uint i1mod3 = (uint)(1 << (int)imod3) & 3;
            return i - imod3 + i1mod3;
        }

        public static uint Cycle3(uint i, uint ofs)
        {
            return i - i % 3 + (i + ofs) % 3;
        }

        public static uint Hash3(uint x, uint y, uint z)
        {
	        //return ( 73856093 * x ) ^ ( 15485867 * y ) ^ ( 83492791 * z );
	        return Murmur32(new List<uint>(){ x, y, z });
        }

        const int THRESH_POINTS_ARE_SAME = 1;

        public static uint HashPoint(Vector3 Point)
        {
            uint x = (uint)System.Math.Floor(Point.x / (2.0f * THRESH_POINTS_ARE_SAME));
            uint y = (uint)System.Math.Floor(Point.y / (2.0f * THRESH_POINTS_ARE_SAME));
            uint z = (uint)System.Math.Floor(Point.z / (2.0f * THRESH_POINTS_ARE_SAME));

	        return Hash3(x, y, z);
        }

        public static uint HashPoint(Vector3 Point, uint Octant)
        {
            uint x = (uint)System.Math.Floor(Point.x / (2.0f * THRESH_POINTS_ARE_SAME) - 0.5f);
            uint y = (uint)System.Math.Floor(Point.y / (2.0f * THRESH_POINTS_ARE_SAME) - 0.5f);
            uint z = (uint)System.Math.Floor(Point.z / (2.0f * THRESH_POINTS_ARE_SAME) - 0.5f);

            x += (Octant >> 0) & 1;
            y += (Octant >> 1) & 1;
            z += (Octant >> 2) & 1;

            return Hash3(x, y, z);
        }
    }
}
