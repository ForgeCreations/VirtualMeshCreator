using System;
using System.Collections.Generic;
using System.Linq;
using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.Utility
{
    public static class TriangleUtils
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

        public static uint Hash(Vector3 e0, Vector3 e1)
        {
            uint h0 = Hash(e0);
            uint h1 = Hash(e1);
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

        public static int CountLeadingZeros(uint i)
        {
            int ret = 0;
            uint temp = ~i;

            while((temp & 0x80000000) > 0)
            {
                temp <<= 1;
                ret++;
            }
            return ret;
        }

        // Separate the original digits with two zeros: 10111 -> 1000001001001, which is used to generate Morton codes
        public static uint ExpandBits(uint v)
        {
            v = (v * 0x00010001u) & 0xFF0000FFu;
            v = (v * 0x00000101u) & 0x0F00F00Fu;
            v = (v * 0x00000011u) & 0xC30C30C3u;
            v = (v * 0x00000005u) & 0x49249249u;
            return v;
        }

        // Morton code requires 0 <= x, y , z <= 1
        public static uint Morton3D(Vector3 p)
        {
            uint x = (uint)p.x * 1023, y = (uint)p.y * 1023, z = (uint)p.z * 1023;
            x = ExpandBits(x);
            y = ExpandBits(y);
            z = ExpandBits(z);
            return (x << 2) | (y << 1) | (z << 1);
        }

        public static uint Morton3DSafe(Vector3 p)
        {
            uint x = (uint)System.Math.Min(System.Math.Max(p.x * 1024.0f, 0.0f), 1023.0f);
            uint y = (uint)System.Math.Min(System.Math.Max(p.y * 1024.0f, 0.0f), 1023.0f);
            uint z = (uint)System.Math.Min(System.Math.Max(p.z * 1024.0f, 0.0f), 1023.0f);
            x = ExpandBits(x);
            y = ExpandBits(y);
            z = ExpandBits(z);
            return (x << 2) | (y << 1) | (z << 1);
        }

        private static ulong BitInterleave(int x, int y, int z)
        {
            return (ulong)((BitSpread(x) << 0) | (BitSpread(y) << 1) | (BitSpread(z) << 2));
        }

        private static int BitExtract(ulong morton, int axis)
        {
            return BitCompact((int)(morton >> axis));
        }

        private static int BitSpread(int x)
        {
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;
            return x;
        }

        private static int BitCompact(int x)
        {
            x &= 0x09249249;
            x = (x ^ (x >> 2)) & 0x030C30C3;
            x = (x ^ (x >> 4)) & 0x0300F00F;
            x = (x ^ (x >> 8)) & 0x030000FF;
            x = (x ^ (x >> 16)) & 0x000003FF;
            return x;
        }

        public static float EqualateralArea(float edgeLength)
        {
            const float sqrt3_4 = 0.4330127f;
            return sqrt3_4 * Mathf.Square(edgeLength * edgeLength);
        }

        public static float EqualateralEdgeLength(float area)
        {
            const float sqrt3_4 = 0.4330127f;
            return (float)System.Math.Sqrt(area / sqrt3_4);
        }

        public static float TriangeArea(float a, float b, float c)
        {
            float areaSquareTime16 = System.Math.Max(0.0f,
                (a + b + c) *
                (-a + b + c) *
                (a - b + c) *
                (a + b - c));
            return (float)System.Math.Sqrt(areaSquareTime16) * 0.25f;
        }

        // a, b, c, are tessellation factors for each edge
        public static int ApproxNumTris(int a, int b, int c)
        {
            // Heron's formula divided by area of unit triangle
            float s = 0.5f * (a + b + c);
            float numTris = 4.0f * (float)System.Math.Sqrt(System.Math.Max(0.0625f, s * (s - a) * (s - b) * (s - c) / 3.0f));
            int maxFactor = Mathf.Max3(a, b, c);
            return System.Math.Max((int)System.Math.Round(numTris), maxFactor);
        }

        #region Barycentric
        // [ Schindler and Chen 2012, "Barycentric Coordinates in Olynpiad Geometrey" https://web.evanchen.cc/handouts/bary/bary-full.pdf]
        public static float LengthSquared(Vector3 barycentrics0, Vector3 barycentrics1, Vector3 edgeLengthsSqr)
        {
            // Barycentric displacment vector:
            // 0 = x + y + z
            Vector3 disp = barycentrics0 - barycentrics1;

            /* TODO change order to match ariel coords
                   v0
                   /\
               e2 /  \ e0
                 /____\
                v2 e1 v1
            */

            // Length of displacment
            return -disp.x * disp.y * edgeLengthsSqr[0]
                   -disp.y * disp.z * edgeLengthsSqr[1]
                   -disp.z * disp.x * edgeLengthsSqr[2];
        }

        public static float SubtriangleArea(Vector3 barycentric0, Vector3 barycentrics1, Vector3 barycentrics2, float triangleArea)
        {
            // Area * Determinant using triple product
            return triangleArea * Vector3.Abs(barycentric0 | (barycentrics1 ^ barycentrics2)).magnitude;
        }

        // https://math.stackexchange.com/questions/3748903/closest-point-to-triangle-edge-with-barycentric-coordinates
        public static float DistanceToEdge(float barycentric, float edgeLength, float triangleArea)
        {
            return 2.0f * barycentric * triangleArea / edgeLength;
        }

        public static float Contangent(Vector3 barycentrics0, Vector3 barycentrics1, Vector3 barycentrics2, Vector3 edgeLengthsSqr, float triangleArea)
        {
            Vector3 lengthsSqr = new Vector3();
            lengthsSqr[0] = LengthSquared(barycentrics1, barycentrics2, edgeLengthsSqr);
            lengthsSqr[1] = LengthSquared(barycentrics2, barycentrics0, edgeLengthsSqr);
            lengthsSqr[2] = LengthSquared(barycentrics0, barycentrics1, edgeLengthsSqr);

            float area = SubtriangleArea(barycentrics0, barycentrics1, barycentrics2, triangleArea);

            return 0.25f * (-lengthsSqr.x + lengthsSqr.y + lengthsSqr.z) / area;
        }
        #endregion

        public static void SubtriangleBarycentrics(uint triX, uint triY, uint flipTri, uint numSubdivisions, ref Vector3[] barycentrics)
        {
            /*
                Vert order:
                1    0__1
                |\   \  |
                | \   \ |  <= flip triangle
                |__\   \|
                0  2    2
            */

            uint[][] vertXY = new uint[3][]
            {
                new uint[2] { triX, triY },
                new uint[2] { triX, triY + 1 },
                new uint[2] { triX + 1, triY }
            };
            vertXY[0][1] += flipTri;
            vertXY[1][0] += flipTri;

            for(int corner = 0; corner < 3; corner++)
            {
                barycentrics[corner][0] = vertXY[corner][0];
                barycentrics[corner][1] = vertXY[corner][1];
                barycentrics[corner][2] = numSubdivisions - vertXY[corner][0] - vertXY[corner][1];
                barycentrics[corner] /= numSubdivisions;
            }
        }

        // Find edge with opposite direction that shares these 2 verts
        /*
              /\
             /  \
            o-<<-0
            o->>-o
             \  /
              \/
        */
        public class EdgeHash
        {
            public HashTable HashTable;

            public EdgeHash(int num)
            {
                HashTable = new HashTable(1u << (int)System.Math.Floor(System.Math.Log(num, 2)), (uint)num);
            }

            public void AddConcurrent(int edgeIndex, Func<int, Vector3> GetPosition)
            {
                Vector3 pos0 = GetPosition(edgeIndex);
                Vector3 pos1 = GetPosition((int)Cycle3((uint)edgeIndex));
                uint hash0 = HashPoint(pos0);
                uint hash1 = HashPoint(pos1);
                uint hash = Murmur32(new List<uint>(2) { hash1, hash0 });
                HashTable.AddConcurrent(hash, edgeIndex);
            }

            public void ForAllMatching(int edgeIndex, bool add, Func<int, Vector3> GetPosition, Func<int, int, int> Function)
            {
                Vector3 pos0 = GetPosition(edgeIndex);
                Vector3 pos1 = GetPosition((int)Cycle3((uint)edgeIndex));
                uint hash0 = HashPoint(pos0);
                uint hash1 = HashPoint(pos1);
                uint hash = Murmur32(new List<uint>(2) { hash0, hash1 });
                for(uint otherEdgeIndex = HashTable.First(hash); HashTable.IsValid(otherEdgeIndex); otherEdgeIndex = HashTable.Next(otherEdgeIndex))
                {
                    if(pos0 == GetPosition((int)Cycle3((uint)edgeIndex)) && pos1 == GetPosition((int)otherEdgeIndex))
                    {
                        // Found matching edge
                        Function(edgeIndex, (int)otherEdgeIndex);
                    }
                }

                if(add)
                    HashTable.Add(Murmur32(new List<uint>(2) { hash0, hash1 }), (uint)edgeIndex);
            }
        }

        public struct FAdjacency
        {
            public int[] Direct;
            public SortedArray<int, int> Extended;

            public FAdjacency(int num)
            {
                Direct = new int[num];
                Extended = new SortedArray<int, int>();
            }

            public void Link(int edgeIndex0, int edgeIndex1)
            {
                if(Direct[edgeIndex0] < 0 && Direct[edgeIndex1] < 0)
                {
                    Direct[edgeIndex0] = edgeIndex1;
                    Direct[edgeIndex1] = edgeIndex0;
                }

                else
                {
                    Extended.Add(edgeIndex0, edgeIndex1);
                    Extended.Add(edgeIndex1, edgeIndex0);
                }
            }

            public void ForAll(int edgeIndex, Func<int, int, int> function)
            {
                int adjIndex = Direct[edgeIndex];
                if(adjIndex >= 0)
                {
                    function(edgeIndex, adjIndex);
                }

                for(var iter = Extended[edgeIndex]; ; ++iter)
                {
                    function(edgeIndex, iter);
                }
            }
        }
    }
}
