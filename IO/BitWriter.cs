using System.Collections.Generic;

namespace VirtualMeshCreator.IO
{
    public class BitWriter
    {
        internal List<byte> m_buffer;
        private byte m_currentByte;
        private int m_numBits;

        public BitWriter()
        {
            m_buffer = new List<byte>();
            m_currentByte = 0;
            m_numBits = 0;
        }

        public void WriteBit(bool bit)
        {
            if(bit)
            {
                m_currentByte |= (byte)(1 << m_numBits);
            }
            m_numBits++;
            if(m_numBits == 8)
            {
                Flush();
            }
        }

        public void WriteBits(int value, int numBits)
        {
            for(int i = 0; i < numBits; i++)
            {
                WriteBit((value & (1 << i)) != 0);
            }
        }

        public byte[] GetBuffer()
        {
            if(m_numBits > 0)
            {
                Flush();
            }
            return m_buffer.ToArray();
        }

        public void Flush()
        {
            if(m_numBits > 0)
            {
                m_buffer.Add(m_currentByte);
                m_currentByte = 0;
                m_numBits = 0;
            }
        }

        public void Write(bool value)
        {
            WriteBit(value);
        }

        public void Write(byte value)
        {
            for(int i = 0; i < 8; i++)
            {
                WriteBit((value & (1 << i)) != 0);
            }
        }

        public void Write(byte[] buffer)
        {
            foreach(var b in buffer)
            {
                Write(b);
            }
        }

        public void Write(uint value, int numBits = 32)
        {
            WriteBits((int)value, numBits);
        }

        public void Write(int value, int numBits = 32, byte offset = 0)
        {
            value >>= offset;
            WriteBits(value, numBits);
        }

        private static byte ConvertToByte(bool[] bools)
        {
            byte result = 0;
            for(int i = 0; i < bools.Length; i++)
            {
                if(bools[i])
                {
                    result |= (byte)(1 << i);
                }
            }
            return result;
        }
    }
}
