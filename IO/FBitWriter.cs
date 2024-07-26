using System.Collections.Generic;

namespace VirtualMeshCreator.IO
{
    public class FBitWriter
    {
        private readonly List<byte> Buffer;
        private ulong PendingBits;
        private int NumPendingBits;

        public FBitWriter(List<byte> buffer)
        {
            Buffer = buffer;
            PendingBits = 0;
            NumPendingBits = 0;
        }

        public void PutBits(uint bits, uint numBits)
        {
            PendingBits |= (ulong)bits << NumPendingBits;
            NumPendingBits += (int)numBits;

            while(NumPendingBits >= 8)
            {
                Buffer.Add((byte)PendingBits);
                PendingBits >>= 8;
                NumPendingBits -= 8;
            }
        }

        public void Flush(uint alignment = 1)
        {
            if(NumPendingBits > 0)
                Buffer.Add((byte)PendingBits);

            while(Buffer.Count % alignment != 0)
                Buffer.Add(0);

            PendingBits = 0;
            NumPendingBits = 0;
        }
    }
}