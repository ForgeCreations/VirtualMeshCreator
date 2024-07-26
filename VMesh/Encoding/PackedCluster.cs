using System;
using System.Diagnostics;
using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.VMesh.Encoding
{
    /// <summary>
    /// Packed Cluster as it is used by the GPU
    /// </summary>
    public class PackedCluster
    {
        //Members needed for rasterization
        public uint NumVerts_PositionOffset;				//NumVerts: 9, PositionOffset: 23
        public uint NumTris_IndexOffset;					//NumTris: 8, IndexOffset: 24
        public uint ColorMin;
        public uint ColorBits_GroupIndex;                   // R: 4, G: 4, B: 4, A: 4. (GroupIndex&0xFFFF) is for debug visualization only.

        public Vector3 PosStart;
        public uint BitsPerIndex_PosPrecision_PosBits;      //BitsPerIndex:4, PosPrecision: 5, PosBits: [X]5. [Y]5. [Z]5

        //Members needed for culling
        public Vector4 LODBounds;                               //LWC_TODO: Was FSphere, but that's now twice as big and won't work on GPU.

        public Vector3 BoxBoundsCenter;
        public uint LODErrorAndEdgeLength;

        public Vector3 BoxBoundsExtent;
        public uint Flags;

        public void SetNumVerts(uint NumVerts)
        {
            SetBits(ref NumVerts_PositionOffset, NumVerts, 9, 0);
        }

        public void SetPositionOffset(uint Offset)
        {
            SetBits(ref NumVerts_PositionOffset, Offset, 23, 9);
        }

        public void SetNumTris(uint NumTris)
        {
            SetBits(ref NumTris_IndexOffset, NumTris, 8, 0);
        }

        public void SetIndexOffset(uint Offset)
        {
            SetBits(ref NumTris_IndexOffset, Offset, 24, 8);
        }

        public void SetGroupIndex(uint GroupIndex)
        {
            SetBits(ref ColorBits_GroupIndex, GroupIndex & 0xFFFFu, 16, 16);
        }

        public void SetColorBitsR(uint NumBits)
        {
            SetBits(ref ColorBits_GroupIndex, NumBits, 4, 0);
        }

        public void SetColorBitsG(uint NumBits)
        {
            SetBits(ref ColorBits_GroupIndex, NumBits, 4, 4);
        }

        public void SetColorBitsB(uint NumBits)
        {
            SetBits(ref ColorBits_GroupIndex, NumBits, 4, 8);
        }

        public void SetColorBitsA(uint NumBits)
        {
            SetBits(ref ColorBits_GroupIndex, NumBits, 4, 12);
        }

        //--------Bit Funcs--------
        private static uint GetBits(uint Value, uint NumBits, uint Offset)
        {
            uint Mask = (1u << (int)NumBits) - 1u;
            return (Value >> (int)Offset) & Mask;
        }

        private static void SetBits(ref uint Value, uint Bits, uint NumBits, uint Offset)
        {
            uint Mask = (1u << (int)NumBits) - 1u;
            Debug.Assert(Bits <= Mask);
            Mask <<= (int)Offset;
            Value = (Value & ~Mask) | (Bits << (int)Offset);
        }
        //-----------End Bit Funcs-----------
    }
}
