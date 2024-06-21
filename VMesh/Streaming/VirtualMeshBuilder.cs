using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.Utility;
using static VirtualMeshCreator.Math.VectorUtility;

namespace VirtualMeshCreator.VMesh.Streaming
{
    public static class VirtualMeshBuilder
    {
        private const int VIRTUALGEOMETRY_STREAMING_PAGE_GPU_SIZE_BITS =    17;
        private const uint VIRTUALGEOMETRY_STREAMING_PAGE_GPU_SIZE =        (1u << VIRTUALGEOMETRY_STREAMING_PAGE_GPU_SIZE_BITS);
        private const uint VIRTUALGEOMETRY_MAX_PAGE_DISK_SIZE =             (VIRTUALGEOMETRY_STREAMING_PAGE_GPU_SIZE * 2);

        private const int CONSTRAINED_CLUSTER_CACHE_SIZE =                  32;
        private const int MIN_PAGE_DISTANCE_FOR_RELATIVE_ENCODING =         4; // Don't use relative encoding near root to avoid small dependent batches for little compression win.

        private const uint INVALID_PART_INDEX =                             0xFFFFFFFFu;
        private const uint INVALID_GROUP_INDEX =                            0xFFFFFFFFu;
        private const uint INVALID_PAGE_INDEX =                             0xFFFFFFFFu;

        private const int VIRTUALGEOMETRY_ROOT_PAGE_GPU_SIZE_BITS =         15;
        private const uint VIRTUALGEOMETRY_ROOT_PAGE_GPU_SIZE =             (1u << VIRTUALGEOMETRY_ROOT_PAGE_GPU_SIZE_BITS);
        private const int VIRTUALGEOMETRY_GPU_PAGE_HEADER_SIZE =            16;

        private const int VIRTUALGEOMETRY_MAX_CLUSTERS_PER_PAGE_BITS =      8;
        private const int VIRTUALGEOMETRY_MAX_CLUSTERS_PER_PAGE_MASK =      ((1 << VIRTUALGEOMETRY_MAX_CLUSTERS_PER_PAGE_BITS) - 1);
        private const int VIRTUALGEOMETRY_MAX_CLUSTERS_PER_PAGE =           (1 << VIRTUALGEOMETRY_MAX_CLUSTERS_PER_PAGE_BITS);
        private const int VIRTUALGEOMETRY_MAX_CLUSTERS_PER_GROUP_BITS =     9;
        private const int VIRTUALGEOMETRY_MAX_CLUSTERS_PER_GROUP_MASK =     ((1 << VIRTUALGEOMETRY_MAX_CLUSTERS_PER_GROUP_BITS) - 1);
        private const int VIRTUALGEOMETRY_MAX_CLUSTERS_PER_GROUP =          ((1 << VIRTUALGEOMETRY_MAX_CLUSTERS_PER_GROUP_BITS) - 1);
        private const int VIRTUALGEOMETRY_MAX_CLUSTERS_PER_GROUP_TARGET =   128;

        private const int VIRTUALGEOMETRY_MAX_CLUSTER_TRIANGLES	=           128;
        private const int VIRTUALGEOMETRY_MAX_CLUSTER_VERTICES_BITS	=       8;
        private const int VIRTUALGEOMETRY_MAX_CLUSTER_VERTICES =            (1 << VIRTUALGEOMETRY_MAX_CLUSTER_VERTICES_BITS);
        private const int VIRTUALGEOMETRY_MAX_CLUSTER_VERTICES_MASK =       ((1 << VIRTUALGEOMETRY_MAX_CLUSTER_VERTICES_BITS) - 1);

        private const int VIRTUALGEOMETRY_MAX_CLUSTER_INDICES =             (VIRTUALGEOMETRY_MAX_CLUSTER_TRIANGLES * 3);
        private const int VIRTUALGEOMETRY_MAX_UVS =                         4;

        private static LRUCache<int, Page> pageCache;

        /*
            Build streaming pages
            Page layout:
                Fixup Chunk (Only loaded to CPU memory)
                FPackedCluster
                (TODO: Use other method to store material info) MaterialRangeTable
                GeometryData
        */

        public static void Build()
        {

        }

        private static void PackCluster(Cluster cluster, out PackedCluster pCluster)
        {
            pCluster = new PackedCluster();

            //0
            pCluster.SetNumVerts((uint)cluster.vertices.ToList().Count);
            pCluster.SetPositionOffset(0);
            pCluster.SetNumTris((uint)cluster.triangles.ToList().Count);
            pCluster.SetIndexOffset(0);
            //OutCluster.ColorMin = EncodingInfo.ColorMin.X | (EncodingInfo.ColorMin.Y << 8) | (EncodingInfo.ColorMin.Z << 16) | (EncodingInfo.ColorMin.W << 24);
            //OutCluster.SetColorBitsR(EncodingInfo.ColorBits.X);
            //OutCluster.SetColorBitsG(EncodingInfo.ColorBits.Y);
            //OutCluster.SetColorBitsB(EncodingInfo.ColorBits.Z);
            //OutCluster.SetColorBitsA(EncodingInfo.ColorBits.W);
            pCluster.SetGroupIndex((uint)cluster.groupID);

            //1
            //OutCluster.PosStart = cluster.QuantizedPosStart;
            //OutCluster.SetBitsPerIndex(EncodingInfo.BitsPerIndex);
            //OutCluster.SetPosPrecision(cluster.QuantizedPosPrecision);
            //OutCluster.SetPosBitsX(cluster.QuantizedPosBits.X);
            //OutCluster.SetPosBitsY(cluster.QuantizedPosBits.Y);
            //OutCluster.SetPosBitsZ(cluster.QuantizedPosBits.Z);

            //2
            pCluster.LODBounds = new Vector4(cluster.lodBounds.center.x, cluster.lodBounds.center.y, cluster.lodBounds.center.z, cluster.lodBounds.radius);

            //3
            pCluster.BoxBoundsCenter = (cluster.boxBounds.Min + cluster.boxBounds.Max) * 0.5f;
            pCluster.LODErrorAndEdgeLength = (uint)((short)cluster.lodError | (short)(cluster.externalEdges.Length << 16));

            //4
            pCluster.BoxBoundsExtent = (cluster.boxBounds.Max - cluster.boxBounds.Min) * 0.5f;

            //5
            //check(NumTexCoords <= VIRTUALGEOMETRY_MAX_UVS);
            //static_assert(VIRTUALGEOMETRY_MAX_UVS <= 4, "UV_Prev encoding only supports up to 4 channels");
            Console.WriteLine("UV_Prev encoding only supports up to 4 channels [" + (VIRTUALGEOMETRY_MAX_UVS <= 4) + "]");
        }

        private static uint CalculateMaxRootPages(uint TargetResidencyInKB)
        {
            ulong SizeInBytes = TargetResidencyInKB << 10;
            return (uint)MathUtil.Clamp((SizeInBytes + VIRTUALGEOMETRY_ROOT_PAGE_GPU_SIZE - 1u) >> VIRTUALGEOMETRY_ROOT_PAGE_GPU_SIZE_BITS, 1u, uint.MaxValue);
        }

        #region Normal Encoding
        private static Vector2 OctahedronEncode(Vector3 N)
        {
            Vector3 AbsN = GetAbs(N);
            float factor = AbsN.x + AbsN.y + AbsN.z;
            N.x /= factor;
            N.y /= factor;
            N.z /= factor;

            if(N.z < 0.0)
            {
                AbsN = GetAbs(N);
                N.x = (N.x >= 0.0f) ? (1.0f - AbsN.y) : (AbsN.y - 1.0f);
                N.y = (N.y >= 0.0f) ? (1.0f - AbsN.x) : (AbsN.x - 1.0f);
            }

            return new Vector2(N.x, N.y);
        }

        private static void OctahedronEncode(Vector3 N, ref int X, ref int Y, int QuantizationBits)
        {
            int QuantizationMaxValue = (1 << QuantizationBits) - 1;
            float Scale = 0.5f * QuantizationMaxValue;
            float Bias = 0.5f * QuantizationMaxValue + 0.5f;

            Vector2 Coord = OctahedronEncode(N);

            X = MathUtil.Clamp((int)(Coord.x * Scale + Bias), 0, QuantizationMaxValue);
            Y = MathUtil.Clamp((int)(Coord.y * Scale + Bias), 0, QuantizationMaxValue);
        }

        private static Vector3 OctahedronDecode(int X, int Y, int QuantizationBits)
        {
            int QuantizationMaxValue = (1 << QuantizationBits) - 1;
            float fx = X * (2.0f / QuantizationMaxValue) - 1.0f;
            float fy = Y * (2.0f / QuantizationMaxValue) - 1.0f;
            float fz = 1.0f - System.Math.Abs(fx) - System.Math.Abs(fy);
            float t = MathUtil.Clamp(-fz, 0.0f, 1.0f);
            fx += (fx >= 0.0f ? -t : t);
            fy += (fy >= 0.0f ? -t : t);
            return GetUnsafeNormal(new Vector3(fx, fy, fz));
        }

        private static void OctahedronEncodePreciseSIMD(Vector3 N, out uint X, out uint Y, int QuantizationBits)
        {
            int QuantizationMaxValue = (1 << QuantizationBits) - 1;
            Vector2 ScalarCoord = OctahedronEncode(N);

            VectorRegister4Float Scale = VectorSetFloat1(0.5f * QuantizationMaxValue);
            VectorRegister4Float RcpScale = VectorSetFloat1(2.0f / QuantizationMaxValue);
            VectorRegister4Int IntCoord = VectorFloatToInt(VectorMultiplyAdd(MakeVectorRegister((uint)ScalarCoord.x, (uint)ScalarCoord.y, (uint)ScalarCoord.x, (uint)ScalarCoord.y), Scale, Scale)); // x0, y0, x1, y1
            IntCoord = VectorIntAdd(IntCoord, MakeVectorRegisterInt(0, 0, 1, 1));
            VectorRegister4Float Coord = VectorMultiplyAdd(VectorIntToFloat(IntCoord), RcpScale, MakeVectorRegisterFloat(1u & 1, 1u & 1, 1u & 1, 1u & 1)/*GlobalVectorConstants::FloatMinusOne*/); // Coord = Coord * 2.0f / QuantizationMaxValue - 1.0f

            VectorRegister4Float Nx = VectorSwizzle(Coord, 0, 2, 0, 2);
            VectorRegister4Float Ny = VectorSwizzle(Coord, 1, 1, 3, 3);
            VectorRegister4Float Nz = VectorSubtract(VectorSubtract(MakeVectorRegisterFloat(1u, 1u, 1u, 1u), VectorAbs(Nx)), VectorAbs(Ny)); // Nz = 1.0f - abs(Nx) - abs(Ny)

            VectorRegister4Float T = VectorMin(Nz, VectorSetFloat1(0.0f)); // T = min(Nz, 0.0f)

            VectorRegister4Float NxSign = VectorBitwiseAnd(Nx, MakeVectorRegisterFloat((uint)System.Math.Sign(1), (uint)System.Math.Sign(1), (uint)System.Math.Sign(1), (uint)System.Math.Sign(1))/*GlobalVectorConstants::SignBit()*/);
            VectorRegister4Float NySign = VectorBitwiseAnd(Ny, MakeVectorRegisterFloat((uint)System.Math.Sign(1), (uint)System.Math.Sign(1), (uint)System.Math.Sign(1), (uint)System.Math.Sign(1))/*GlobalVectorConstants::SignBit()*/);

            Nx = VectorAdd(Nx, VectorBitwiseXor(T, NxSign)); // Nx += T ^ NxSign
            Ny = VectorAdd(Ny, VectorBitwiseXor(T, NySign)); // Ny += T ^ NySign

            VectorRegister4Float Dots = VectorMultiplyAdd(Nx, VectorSetFloat1(N.x), VectorMultiplyAdd(Ny, VectorSetFloat1(N.y), VectorMultiply(Nz, VectorSetFloat1(N.z))));
            VectorRegister4Float Lengths = VectorSqrt(VectorMultiplyAdd(Nx, Nx, VectorMultiplyAdd(Ny, Ny, VectorMultiply(Nz, Nz))));
            Dots = VectorDivide(Dots, Lengths);

            VectorRegister4Float Mask = MakeVectorRegister(0xFFFFFFFCu, 0xFFFFFFFCu, 0xFFFFFFFCu, 0xFFFFFFFCu);
            VectorRegister4Float LaneIndices = MakeVectorRegister(0u, 1u, 2u, 3u);
            Dots = VectorBitwiseOr(VectorBitwiseAnd(Dots, Mask), LaneIndices);

            //Calculate max component
            VectorRegister4Float MaxDot = VectorMax(Dots, VectorSwizzle(Dots, 2, 3, 0, 1));
            MaxDot = VectorMax(MaxDot, VectorSwizzle(MaxDot, 1, 2, 3, 0));

            uint Index = (uint)(0xFFFFFFF & (int)MaxDot.V[0]);

            int[] IntCoordValues = new int[4];
            VectorIntStore(IntCoord, ref IntCoordValues);
            X = (uint)MathUtil.Clamp(IntCoordValues[0] + ((int)Index & 1), 0, QuantizationMaxValue);
            Y = (uint)MathUtil.Clamp(IntCoordValues[1] + (((int)Index >> 1) & 1), 0, QuantizationMaxValue);
        }

        private static void OctahedronEncodePrecise(Vector3 N, ref int X, ref int Y, int QuantizationBits)
        {
            int QuantizationMaxValue = (1 << QuantizationBits) - 1;
            Vector2 Coord = OctahedronEncode(N);

            float Scale = 0.5f * QuantizationMaxValue;
            float Bias = 0.5f * QuantizationMaxValue;
            int NX = MathUtil.Clamp((int)(Coord.x * Scale + Bias), 0, QuantizationMaxValue);
            int NY = MathUtil.Clamp((int)(Coord.y * Scale + Bias), 0, QuantizationMaxValue);

            float MinError = 1.0f;
            int BestNX = 0;
            int BestNY = 0;
            for(int OffsetY = 0; OffsetY < 2; OffsetY++)
            {
                for(int OffsetX = 0; OffsetX < 2; OffsetX++)
                {
                    int TX = NX + OffsetX;
                    int TY = NY + OffsetY;
                    if(TX <= QuantizationMaxValue && TY <= QuantizationMaxValue)
                    {
                        Vector3 RN = OctahedronDecode(TX, TY, QuantizationBits);
                        //float Error = System.Math.Abs(1.0f - (RN | N));
                        float Error = GetAbs(Vector3.one - Or(RN, N)).magnitude;
                        if(Error < MinError)
                        {
                            MinError = Error;
                            BestNX = TX;
                            BestNY = TY;
                        }
                    }
                }
            }

            X = BestNX;
            Y = BestNY;
        }

        private static uint PackNormal(Vector3 Normal, int QuantizationBits)
        {
            uint X, Y;
            OctahedronEncodePreciseSIMD(Normal, out X, out Y, QuantizationBits);
            return (Y << QuantizationBits) | X;
        }
        #endregion

        #region Compression
        private static byte[] LZCompress(byte[] data)
        {
            List<byte> compressedData = new List<byte>();
            int length = data.Length;
            int pos = 0;

            while(pos < length)
            {
                int matchLength = 1;
                int matchDistance = 0;

                for(int i =1; i < System.Math.Min(pos, 65536); i++)
                {
                    int maxMatchLength = System.Math.Min(256, length - pos);
                    int j = 0;

                    while(j < maxMatchLength && data[pos - i + j] == data[pos + j])
                    {
                        j++;
                    }

                    if(j > matchLength)
                    {
                        matchLength = j;
                        matchDistance = i;
                    }
                }

                if(matchLength >= 3)
                {
                    compressedData.Add((byte)(0b10000000 | (matchLength - 3)));
                    compressedData.Add((byte)(matchDistance & 0xFF));
                    compressedData.Add((byte)((matchDistance >> 8) & 0xFF));
                    pos += matchLength;
                }

                else
                {
                    compressedData.Add(data[pos]);
                    pos++;
                }
            }

            return compressedData.ToArray();
        }

        private static byte[] LZDecompress(byte[] compressedData)
        {
            List<byte> decompressedData = new List<byte>();
            int pos = 0;

            while(pos < compressedData.Length)
            {
                byte flag = compressedData[pos];
                if((flag & 0b10000000) != 0)
                {
                    int matchLength = (flag & 0b10000000) + 3;
                    int matchDistance = compressedData[pos + 1] | (compressedData[pos + 2] << 8);
                    int matchStart = decompressedData.Count - matchDistance;

                    for(int i = 0; i < matchLength; i++)
                    {
                        decompressedData.Add(decompressedData[matchStart + i]);
                    }

                    pos += 3;
                }

                else
                {
                    decompressedData.Add(flag);
                    pos++;
                }
            }

            return decompressedData.ToArray();
        }
        #endregion
    }
}
