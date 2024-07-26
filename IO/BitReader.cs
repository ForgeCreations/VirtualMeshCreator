namespace VirtualMeshCreator.IO
{
    public class BitReader
    {
        public byte[] m_buffer;
        public int m_currentByteIndex;
        public int m_currentBitIndex;

        public BitReader(byte[] buffer, int byteOffset = 0)
        {
            m_buffer = buffer;
            m_currentByteIndex = byteOffset;
            m_currentBitIndex = 0;
        }

        public bool ReadBit()
        {
            bool bit = (m_buffer[m_currentByteIndex] & (1 << m_currentBitIndex)) != 0;
            m_currentBitIndex++;
            if(m_currentBitIndex == 8)
            {
                m_currentBitIndex = 0;
                m_currentByteIndex++;
            }
            return bit;
        }

        public int ReadBits(int numBits, int offset = 0, int value = 0)
        {
            for(int i = 0; i < numBits; i++)
            {
                if(ReadBit())
                {
                    value |= (1 << (i + offset));
                }
            }
            return value;
        }

        public bool ReadBoolean()
        {
            return ReadBit();
        }

        public byte ReadByte()
        {
            byte value = 0;
            for(int i = 0; i < 8; i++)
            {
                if(ReadBit())
                {
                    value |= (byte)(1 << i);
                }
            }
            return value;
        }

        public byte[] ReadBytes(int count)
        {
            byte[] result = new byte[count];
            for(int i = 0; i < count; i++)
            {
                result[i] = ReadByte();
            }
            return result;
        }

        public ushort ReadUInt16()
        {
            ushort value = 0;
            for(int i = 0; i < 16; i++)
            {
                if(ReadBit())
                {
                    value |= (ushort)(1 << i);
                }
            }
            return value;
        }

        public uint ReadUInt32()
        {
            uint value = 0;
            for(int i = 0; i < 32; i++)
            {
                if(ReadBit())
                {
                    value |= (1U << i);
                }
            }
            return value;
        }

        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            ulong value = 0;
            for(int i = 0; i < 64; i++)
            {
                if(ReadBit())
                {
                    value |= (1UL << i);
                }
            }
            return value;
        }
    }
}
