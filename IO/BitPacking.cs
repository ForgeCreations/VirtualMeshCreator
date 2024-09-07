using System;
using VirtualMeshCreator.Math.Internal;

namespace VirtualMeshCreator.IO
{
    /// <summary>
    /// Utility functions for packing bits into uints.
    /// </summary>
    public static class BitPacking
    {
        public static uint BitFieldExtractU32(uint value, int numBits, int bitOffset)
        {
            return (value >> bitOffset) & ((1u << numBits) - 1u);
        }

        public static uint3 UnpackToUint3(uint value, int3 numComponentBits)
        {
            return new uint3(
                BitFieldExtractU32(value, numComponentBits.x, 0),
                BitFieldExtractU32(value, numComponentBits.y, numComponentBits.x),
                BitFieldExtractU32(value, numComponentBits.z, numComponentBits.x + numComponentBits.y)
            );
        }

        public static uint4 UnpackToUint4(uint value, int4 numComponentBits)
        {
            return new uint4(
                BitFieldExtractU32(value, numComponentBits.x, 0),
                BitFieldExtractU32(value, numComponentBits.y, numComponentBits.x),
                BitFieldExtractU32(value, numComponentBits.z, numComponentBits.x + numComponentBits.y),
                BitFieldExtractU32(value, numComponentBits.w, numComponentBits.x + numComponentBits.y + numComponentBits.z)
            );
        }

        public static float4 Saturate(float4 value)
        {
            return new float4(
                (float)System.Math.Min(1.0f, (float)System.Math.Max(0.0f, value.x)),
                (float)System.Math.Min(1.0f, (float)System.Math.Max(0.0f, value.y)),
                (float)System.Math.Min(1.0f, (float)System.Math.Max(0.0f, value.z)),
                (float)System.Math.Min(1.0f, (float)System.Math.Max(0.0f, value.w))
            );
        }

        public static float4 Unpack_R10G10B10A2_UNORM_To_Float4(uint packed)
        {
            return new float4(
                (float)(packed & 0x000003FF) / 1023,
                (float)((packed >> 10) & 0x000003FF) / 1023,
                (float)((packed >> 20) & 0x000003FF) / 1023,
                (float)((packed >> 30) & 0x00000003) / 3
            );
        }

        public static uint BitFieldMaskU32(uint numBits, uint bitOffset)
        {
            return ((1u << (int)numBits) - 1u) << (int)bitOffset;
        }

        public static void PutBits(byte[] output, uint alignedBaseAddress, uint bitOffset, uint value, uint numBits)
        {
            uint bitOffsetInDword = bitOffset & 31u;

            uint bits = value << (int)bitOffsetInDword;
            uint address = alignedBaseAddress + ((bitOffset >> 5) << 2);
            uint endBitPos = bitOffsetInDword + numBits;

            if(endBitPos >= 32)
            {
                uint mask = 0xFFFFFFFFu << (int)(endBitPos & 31u);
                InterlockedAnd(ref output, address + 4, mask);
                InterlockedOr(ref output, address + 4, value >> (int)(32 - bitOffsetInDword));
            }

            {
                uint mask = ~BitFieldMaskU32(numBits, bitOffset);
                InterlockedAnd(ref output, address, mask);
                InterlockedOr(ref output, address, value << (int)bitOffsetInDword);
            }
        }

        public static void InterlockedAnd(ref byte[] output, uint address, uint mask)
        {
            uint value = BitConverter.ToUInt32(output, (int)address);
            value &= mask;
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, output, (int)address, bytes.Length);
        }

        public static void InterlockedOr(ref byte[] output, uint address, uint value)
        {
            uint current = BitConverter.ToUInt32(output, (int)address);
            current |= value;
            byte[] bytes = BitConverter.GetBytes(current);
            Array.Copy(bytes, 0, output, (int)address, bytes.Length);
        }

        // When Position and NumBits can be determined at compile time this should be just as fast as manual bit packing.
        public static uint ReadBits(uint4 data, ref uint position, uint numBits)
        {
            uint dwordIndex = position >> 5;
            uint bitIndex = position & 31;

            uint value = data[dwordIndex] >> (int)bitIndex;
            if(bitIndex + numBits > 32)
            {
                value |= data[dwordIndex + 1] << (int)(32 - bitIndex);
            }

            position += numBits;

            uint mask = (1u << (int)numBits) - 1u;
            return value & mask;
        }

        public static void WriteBits(ref uint4 data, ref uint position, uint value, uint numBits)
        {
            uint dwordIndex = position >> 5;
            uint bitIndex = position & 31;

            data[dwordIndex] |= value << (int)bitIndex;
            if(bitIndex + numBits > 32)
            {
                data[dwordIndex + 1] |= value >> (int)(32 - bitIndex);
            }

            position += numBits;
        }
    }
}
