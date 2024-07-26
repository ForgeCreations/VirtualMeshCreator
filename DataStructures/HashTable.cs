using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;

namespace VirtualMeshCreator.Utility
{
    public class HashTable
    {
        private uint hashSize;
        private uint hashMask;
        private uint indexSize;
        private uint[] hash;
        private uint[] nextIndex;

        public HashTable(uint indexSize = 0)
        {
            hash = null;
            nextIndex = null;
            Resize(indexSize);
        }

        public HashTable(uint hashSize, uint indexSize)
        {
            hash = null;
            nextIndex = null;
            Resize(hashSize, indexSize);
        }

        ~HashTable()
        {
            Free();
        }

        private void Resize(uint indexSize)
        {
            Resize(MeshUtility.LowerNearest2Power(indexSize), indexSize);
        }

        private void Resize(uint hashSize, uint indexSize)
        {
            Free();
            if((hashSize & (hashSize - 1)) != 0)
                throw new ArgumentException("Hash size must be a power of two.");

            this.hashSize = hashSize;
            hashMask = hashSize - 1;
            this.indexSize = indexSize;
            hash = new uint[hashSize];
            nextIndex = new uint[indexSize];
            hash.Fill(uint.MaxValue);
        }

        private void ResizeIndex(uint indexSize)
        {
            uint[] newIndexes = new uint[indexSize];
            Array.Copy(nextIndex, newIndexes, this.indexSize);
            nextIndex = newIndexes;
            this.indexSize = indexSize;
        }

        public void Clear()
        {
            hash.Fill(uint.MaxValue);
        }

        public void Free()
        {
            hashSize = 0;
            hashMask = 0;
            indexSize = 0;
            hash = null;
            nextIndex = null;
        }

        public void Add(uint key, uint idx)
        {
            if(idx >= indexSize)
            {
                ResizeIndex(MeshUtility.UpperNearest2Power(idx + 1));
            }
            key &= hashMask;
            nextIndex[idx] = hash[key];
            hash[key] = idx;
        }

        // Safe for many threads to add concurrently.
        // Not safe to search the table while other threads are adding.
        // Will not resize. Only use for presized tables.
        public void AddConcurrent(uint key, int idx)
        {
            key &= hashMask;
            //next_index[idx] = FPlatformAtomics::InterlockedExchange((int)hash[key], idx);
            nextIndex[idx] = hash[key];
        }

        public void Remove(uint key, uint idx)
        {
            if(idx >= indexSize) return;
            key &= hashMask;
            if(hash[key] == idx) hash[key] = nextIndex[idx];

            else
            {
                for(uint i = hash[key]; i != uint.MaxValue; i = nextIndex[i])
                {
                    if(nextIndex[i] == idx)
                    {
                        nextIndex[i] = nextIndex[idx];
                        break;
                    }
                }
            }
        }

        public void Clear(uint inHashSize, uint inIndexSize)
        {
            Free();

            hashSize = inHashSize;
            indexSize = inIndexSize;

            //check(HashSize > 0);
            //Console.WriteLine(hashSize > 0);
            //check(FMath::IsPowerOfTwo(HashSize));
            //Console.WriteLine(MathUtil.IsPowerOfTwo(hashSize));

            if(indexSize > 0)
            {
                hashMask = hashSize - 1;

                hash = new uint[hashSize];
                nextIndex = new uint[indexSize];

                hash.Fill(uint.MaxValue);
            }
        }

        public uint First(uint key)
        {
            key &= hashMask;
            return hash[key];
        }

        public bool IsValid(uint idx)
        {
            return idx != uint.MaxValue;
        }

        public uint Next(uint Index)
        {
            //checkSlow(Index < IndexSize);
            //Console.WriteLine(Index < index_size);
            //checkSlow(NextIndex[Index] != Index); // check for corrupt tables
            //Console.WriteLine("Corrupt Tables? [" + (next_index[Index] != Index) + "]");
            return nextIndex[Index];
        }

        public struct Container : IEnumerable<uint>
        {
            public uint idx;
            public uint[] next;

            public IEnumerator<uint> GetEnumerator()
            {
                for(uint i = idx; i != uint.MaxValue; i = next[i])
                {
                    yield return i;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public Container this[uint key]
        {
            get
            {
                if(hashSize == 0 || indexSize == 0) return new Container { idx = uint.MaxValue, next = null };
                key &= hashMask;
                return new Container { idx = hash[key], next = nextIndex };
            }
        }
    }
}
