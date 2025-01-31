using System.Collections.Generic;

namespace VirtualMeshCreator.IO
{
    public static class Compression
    {
        #region LZ Compression
        internal static byte[] LZCompress(byte[] data)
        {
            List<byte> compressedData = new List<byte>();
            int length = data.Length;
            int pos = 0;

            while(pos < length)
            {
                int matchLength = 1;
                int matchDistance = 0;

                for(int i = 1; i < System.Math.Min(pos, 65536); i++)
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

        internal static byte[] LZDecompress(byte[] compressedData)
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

        #region BCC Compression

        #endregion

        #region ECT5 Compression

        #endregion
    }
}
