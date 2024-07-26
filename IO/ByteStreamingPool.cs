using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.VMesh.Encoding;

namespace VirtualMeshCreator.IO
{
    public class ByteStreamingPool : IDisposable
    {
        private readonly int _poolSize;
        private readonly int _chunkSize;
        private readonly byte[][] _pool;
        private readonly ConcurrentDictionary<long, byte[]> _cache;
        private readonly int _cacheLimit;
        private long _nextAddress;
        private readonly object _lock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the ByteStreamingPool.
        /// </summary>
        /// <param name="poolSize">The total number of chunks in the pool.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <param name="cacheLimit">The maximum number of cached chunks before eviction occurs.</param>
        public ByteStreamingPool(int poolSize, int chunkSize = 256, int cacheLimit = 64)
        {
            _poolSize = poolSize;
            _chunkSize = chunkSize;
            _pool = new byte[_poolSize][];
            _cache = new ConcurrentDictionary<long, byte[]>(Environment.ProcessorCount / 2, cacheLimit);
            _cacheLimit = cacheLimit;
            _nextAddress = 0;

            for(int i = 0; i < _poolSize; i++)
            {
                _pool[i] = new byte[_chunkSize];
            }
        }

        /// <summary>
        /// Adds a value to the stream, optionally quantizing `Vector3` values based on the specified precision.
        /// </summary>
        /// <param name="value">The value to be added to the stream. Can be byte, ushort, uint, ulong, or Vector3.</param>
        /// <param name="precision">The precision for quantizing the Vector3 value. Ignored if quantize is false. Default is StepSize1cm.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task AddDataToStreamAsync(object value, PositionPrecision precision = PositionPrecision.StepSize1cm)
        {
            byte[] data;
            switch(value)
            {
                case byte byteValue:
                    data = new byte[] { byteValue };
                    break;
                case ushort ushortValue:
                    data = BitConverter.GetBytes(ushortValue);
                    break;
                case uint uintValue:
                    data = BitConverter.GetBytes(uintValue);
                    break;
                case ulong ulongValue:
                    data = BitConverter.GetBytes(ulongValue);
                    break;
                case Vector3 vertex:
                    data = QuantizeVertex(vertex, precision);
                    break;
                default:
                    throw new ArgumentException("Unsupported data type");
            }
            byte[] transcodedData = Compression.LZCompress(data);
            await EncodeAsync(transcodedData);
        }

        /// <summary>
        /// Encodes the provided byte array into the pool and returns an address for retrieval.
        /// </summary>
        /// <param name="data">The byte array to be encoded.</param>
        /// <returns>A Task that represents the asynchronous operation. The task result contains the address where the data is stored.</returns>
        public async Task<long> EncodeAsync(byte[] data)
        {
            if(data.Length > _chunkSize)
                throw new ArgumentException("Data size exceeds chunk size");

            long address;

            lock(_lock)
            {
                address = _nextAddress++;
            }

            byte[] chunk = _pool[address % _poolSize];

            int bitPackedData = BitPack(data);
            BitConverter.GetBytes(bitPackedData).CopyTo(chunk, 0);

            lock(_lock)
            {
                if(_cache.Count >= _cacheLimit)
                {
                    _cache.Clear();
                }

                _cache[address] = chunk;
            }

            return await Task.FromResult(address);
        }

        /// <summary>
        /// Decodes the byte data from the pool using the specified address.
        /// </summary>
        /// <param name="address">The address where the data is stored.</param>
        /// <returns>A Task that represents the asynchronous operation. The task result contains the decoded byte array.</returns>
        public async Task<byte[]> DecodeAsync(long address)
        {
            if(_cache.TryGetValue(address, out byte[] cachedChunk))
            {
                return await Task.FromResult(UnpackBits(cachedChunk));
            }

            byte[] chunk = _pool[address % _poolSize];
            byte[] result = UnpackBits(chunk);

            lock(_lock)
            {
                _cache[address] = result;
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Retrieves a portion of data from the pool at a specified address.
        /// </summary>
        /// <param name="address">The address where the data is stored.</param>
        /// <param name="offset">The offset in the chunk to start reading from.</param>
        /// <param name="length">The length of the data to read.</param>
        /// <returns>A Task that represents the asynchronous operation. The task result contains the byte array of the requested data.</returns>
        public async Task<byte[]> RandomAccessAsync(long address, int offset, int length)
        {
            if(_cache.TryGetValue(address, out byte[] cachedChunk))
            {
                byte[] result = new byte[length];
                Array.Copy(cachedChunk, offset, result, 0, length);
                return await Task.FromResult(result);
            }

            byte[] chunk = _pool[address % _poolSize];
            byte[] data = new byte[length];
            Array.Copy(chunk, offset, data, 0, length);
            return await Task.FromResult(data);
        }

        private int BitPack(byte[] data)
        {
            int result = 0;
            for(int i = 0; i < data.Length; i++)
            {
                result |= data[i] << (i * 8);
            }
            return result;
        }

        private byte[] UnpackBits(byte[] packedData)
        {
            int packedInt = BitConverter.ToInt32(packedData, 0);
            byte[] unpackedData = new byte[4];
            for(int i = 0; i < 4; i++)
            {
                unpackedData[i] = (byte)((packedInt >> (i * 8)) & 0xFF);
            }
            return unpackedData;
        }

        private int DetermineOptimalBitLength(Vector3 vector)
        {
            float maxDimension = System.Math.Max(vector.x, System.Math.Max(vector.y, vector.z));
            float minDimension = System.Math.Min(vector.x, System.Math.Min(vector.y, vector.z));
            float range = maxDimension - minDimension;

            int bitLength = 4;
            if(range > 0.64f) bitLength = 8;
            else if(range > 0.32f) bitLength = 7;
            else if(range > 0.16f) bitLength = 6;
            else if(range > 0.08f) bitLength = 5;
            else if(range > 0.04f) bitLength = 4;

            return bitLength;
        }

        /// <summary>
        /// Clears the stream by resetting the pool and cache.
        /// </summary>
        public void ClearStream()
        {
            lock(_lock)
            {
                for(int i = 0; i < _poolSize; i++)
                {
                    Array.Clear(_pool[i], 0, _chunkSize);
                }
                _cache.Clear();
                _nextAddress = 0;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            // Used for manual disposal
            //GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the ByteStreamingPool instance and releases all resources.
        /// </summary>
        /// <param name="disposing">A boolean indicating whether the method was called from Dispose method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                if(disposing)
                {
                    ClearStream();
                }
                _disposed = true;
            }
        }

        #region Extra
        /// <summary>
        /// Quantizes a Vector3 value based on the specified precision.
        /// </summary>
        /// <param name="value">The Vector3 value to be quantized.</param>
        /// <param name="precision">The precision for quantization, determining the bit length.</param>
        /// <returns>The quantized Vector3 value.</returns>
        private byte[] QuantizeVertex(Vector3 value, PositionPrecision precision)
        {
            int bitLength;
            switch(precision)
            {
                case PositionPrecision.StepSize1cm:
                    bitLength = 9;
                    break;
                case PositionPrecision.StepSize2cm:
                    bitLength = 8;
                    break;
                case PositionPrecision.StepSize4cm:
                    bitLength = 7;
                    break;
                case PositionPrecision.StepSize8cm:
                    bitLength = 6;
                    break;
                case PositionPrecision.StepSize16cm:
                    bitLength = (byte)(precision - 1);
                    break;
                case PositionPrecision.StepSize32cm:
                    bitLength = 5;
                    break;
                case PositionPrecision.StepSize64cm:
                    bitLength = 4;
                    break;
                case PositionPrecision.Auto:
                default:
                    bitLength = DetermineOptimalBitLength(value);
                    break;
            }

            Vector3 minCoords = Vector3.zero;
            Vector3 maxCoords = Vector3.one;
            Vector3 scale = (maxCoords - minCoords) / ((1 << bitLength) - 1);

            Vector3 quantizedVertex = new Vector3(
                (float)System.Math.Round(value.x / minCoords.x) * scale.x,
                (float)System.Math.Round(value.y / minCoords.y) * scale.y,
                (float)System.Math.Round(value.z / minCoords.z) * scale.z
            );

            byte[] quantizedBytes = new byte[3 * sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes(quantizedVertex.x), 0, quantizedBytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quantizedVertex.y), 0, quantizedBytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quantizedVertex.z), 0, quantizedBytes, 2 * sizeof(float), sizeof(float));
            return quantizedBytes;
        }
        #endregion
    }
}
